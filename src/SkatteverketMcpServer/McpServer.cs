using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;
using SkatteverketMcpServer.Prompts;
using SkatteverketMcpServer.Resources;
using SkatteverketMcpServer.Tools;
using SkatteverketMcpServer.Transport;

namespace SkatteverketMcpServer;

/// <summary>
/// Main MCP Server implementing JSON-RPC protocol
/// </summary>
public class McpServer
{
    private readonly StdioTransport _transport;
    private readonly VatDraftTools _draftTools;
    private readonly VatSubmissionTools _submissionTools;
    private readonly VatResources _resources;
    private readonly VatPrompts _prompts;
    private readonly ILogger<McpServer> _logger;
    private bool _initialized = false;

    public McpServer(
        StdioTransport transport,
        VatDraftTools draftTools,
        VatSubmissionTools submissionTools,
        VatResources resources,
        VatPrompts prompts,
        ILogger<McpServer> logger)
    {
        _transport = transport;
        _draftTools = draftTools;
        _submissionTools = submissionTools;
        _resources = resources;
        _prompts = prompts;
        _logger = logger;
    }

    /// <summary>
    /// Start the MCP server and handle incoming requests
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Skatteverket MCP Server...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _transport.ReadMessageAsync(cancellationToken);

                if (message == null)
                {
                    _logger.LogInformation("End of input stream, shutting down");
                    break;
                }

                await HandleMessageAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in server loop");
            throw;
        }
    }

    private async Task HandleMessageAsync(JsonDocument message, CancellationToken cancellationToken)
    {
        try
        {
            var root = message.RootElement;

            // Get request ID (can be null for notifications)
            object? id = null;
            if (root.TryGetProperty("id", out var idElement))
            {
                id = idElement.ValueKind switch
                {
                    JsonValueKind.Number => idElement.GetInt64(),
                    JsonValueKind.String => idElement.GetString(),
                    _ => null
                };
            }

            // Get method name
            if (!root.TryGetProperty("method", out var methodElement))
            {
                await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidRequest, "Missing method property");
                return;
            }

            var method = methodElement.GetString();
            if (string.IsNullOrEmpty(method))
            {
                await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidRequest, "Invalid method");
                return;
            }

            // Get parameters
            Dictionary<string, object>? parameters = null;
            if (root.TryGetProperty("params", out var paramsElement))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText());
            }

            _logger.LogDebug("Handling method: {Method}", method);

            // Route to appropriate handler
            await HandleMethodAsync(method, parameters, id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
            await _transport.SendErrorAsync(null, JsonRpcErrorCodes.InternalError, ex.Message);
        }
    }

    private async Task HandleMethodAsync(string method, Dictionary<string, object>? parameters, object? id, CancellationToken cancellationToken)
    {
        try
        {
            switch (method)
            {
                case "initialize":
                    await HandleInitializeAsync(parameters, id, cancellationToken);
                    break;

                case "initialized":
                    // Notification from client that initialization is complete
                    _logger.LogInformation("Client initialization complete");
                    break;

                case "tools/list":
                    await HandleToolsListAsync(id, cancellationToken);
                    break;

                case "tools/call":
                    await HandleToolsCallAsync(parameters, id, cancellationToken);
                    break;

                case "resources/list":
                    await HandleResourcesListAsync(id, cancellationToken);
                    break;

                case "resources/read":
                    await HandleResourcesReadAsync(parameters, id, cancellationToken);
                    break;

                case "prompts/list":
                    await HandlePromptsListAsync(id, cancellationToken);
                    break;

                case "prompts/get":
                    await HandlePromptsGetAsync(parameters, id, cancellationToken);
                    break;

                case "ping":
                    await _transport.SendResponseAsync(id, new { }, cancellationToken);
                    break;

                default:
                    await _transport.SendErrorAsync(id, JsonRpcErrorCodes.MethodNotFound, $"Unknown method: {method}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling method {Method}", method);
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InternalError, ex.Message);
        }
    }

    private async Task HandleInitializeAsync(Dictionary<string, object>? parameters, object? id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling initialize request");

        var response = new McpInitializeResponse
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = false },
                Resources = new ResourcesCapability { Subscribe = false, ListChanged = true },
                Prompts = new PromptsCapability { ListChanged = false }
            },
            ServerInfo = new ServerInfo
            {
                Name = "Skatteverket MCP Server",
                Version = "1.0.0"
            }
        };

        _initialized = true;
        await _transport.SendResponseAsync(id, response, cancellationToken);
    }

    private async Task HandleToolsListAsync(object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        var tools = new List<McpTool>();
        tools.AddRange(_draftTools.GetToolDefinitions());
        tools.AddRange(_submissionTools.GetToolDefinitions());

        var response = new { tools };
        await _transport.SendResponseAsync(id, response, cancellationToken);
    }

    private async Task HandleToolsCallAsync(Dictionary<string, object>? parameters, object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        if (parameters == null || !parameters.ContainsKey("name"))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Missing tool name");
            return;
        }

        var toolName = parameters["name"].ToString();
        if (string.IsNullOrEmpty(toolName))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Invalid tool name");
            return;
        }

        Dictionary<string, object>? arguments = null;
        if (parameters.ContainsKey("arguments") && parameters["arguments"] is JsonElement argsElement)
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText());
        }

        ToolCallResponse result;

        // Route to appropriate tool handler
        if (toolName.StartsWith("get_vat_draft") || toolName.Contains("draft") || toolName.Contains("lock") || toolName.Contains("unlock") || toolName == "validate_vat_draft")
        {
            result = await _draftTools.ExecuteToolAsync(toolName, arguments, cancellationToken);
        }
        else
        {
            result = await _submissionTools.ExecuteToolAsync(toolName, arguments, cancellationToken);
        }

        await _transport.SendResponseAsync(id, result, cancellationToken);
    }

    private async Task HandleResourcesListAsync(object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        var resourcesList = await _resources.GetResourceListAsync(cancellationToken);
        var response = new { resources = resourcesList };
        await _transport.SendResponseAsync(id, response, cancellationToken);
    }

    private async Task HandleResourcesReadAsync(Dictionary<string, object>? parameters, object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        if (parameters == null || !parameters.ContainsKey("uri"))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Missing resource URI");
            return;
        }

        var uri = parameters["uri"].ToString();
        if (string.IsNullOrEmpty(uri))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Invalid resource URI");
            return;
        }

        try
        {
            var content = await _resources.ReadResourceAsync(uri, cancellationToken);
            var response = new { contents = new[] { content } };
            await _transport.SendResponseAsync(id, response, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.ResourceNotFound, ex.Message);
        }
    }

    private async Task HandlePromptsListAsync(object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        var promptsList = _prompts.GetPromptDefinitions();
        var response = new { prompts = promptsList };
        await _transport.SendResponseAsync(id, response, cancellationToken);
    }

    private async Task HandlePromptsGetAsync(Dictionary<string, object>? parameters, object? id, CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.McpError, "Server not initialized");
            return;
        }

        if (parameters == null || !parameters.ContainsKey("name"))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Missing prompt name");
            return;
        }

        var promptName = parameters["name"].ToString();
        if (string.IsNullOrEmpty(promptName))
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.InvalidParams, "Invalid prompt name");
            return;
        }

        Dictionary<string, object>? arguments = null;
        if (parameters.ContainsKey("arguments") && parameters["arguments"] is JsonElement argsElement)
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText());
        }

        try
        {
            var messages = _prompts.GetPromptMessages(promptName, arguments);
            var response = new { messages };
            await _transport.SendResponseAsync(id, response, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await _transport.SendErrorAsync(id, JsonRpcErrorCodes.PromptNotFound, ex.Message);
        }
    }
}
