using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;
using SkatteverketMcpServer.Services;

namespace SkatteverketMcpServer.Tools;

/// <summary>
/// MCP Tools for VAT submission and decision operations
/// </summary>
public class VatSubmissionTools
{
    private readonly ISkatteverketApiClient _apiClient;
    private readonly ILogger<VatSubmissionTools> _logger;

    public VatSubmissionTools(ISkatteverketApiClient apiClient, ILogger<VatSubmissionTools> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all tool definitions
    /// </summary>
    public List<McpTool> GetToolDefinitions()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "get_vat_submissions",
                Description = "Retrieve all submitted VAT declarations for the authenticated user",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            },
            new McpTool
            {
                Name = "get_vat_submission",
                Description = "Get a specific submitted VAT declaration by redovisare and period",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["redovisare"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The tax reporter ID"
                        },
                        ["period"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The reporting period"
                        }
                    },
                    Required = new List<string> { "redovisare", "period" }
                }
            },
            new McpTool
            {
                Name = "get_vat_decisions",
                Description = "Retrieve all tax decisions for the authenticated user",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            },
            new McpTool
            {
                Name = "get_vat_decision",
                Description = "Get a specific tax decision by redovisare and period",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["redovisare"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The tax reporter ID"
                        },
                        ["period"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The reporting period"
                        }
                    },
                    Required = new List<string> { "redovisare", "period" }
                }
            },
            new McpTool
            {
                Name = "health_check",
                Description = "Check connectivity and health status of Skatteverket API",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            }
        };
    }

    /// <summary>
    /// Execute a tool by name
    /// </summary>
    public async Task<ToolCallResponse> ExecuteToolAsync(string toolName, Dictionary<string, object>? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", toolName);

            return toolName switch
            {
                "get_vat_submissions" => await GetVatSubmissionsAsync(cancellationToken),
                "get_vat_submission" => await GetVatSubmissionAsync(arguments, cancellationToken),
                "get_vat_decisions" => await GetVatDecisionsAsync(cancellationToken),
                "get_vat_decision" => await GetVatDecisionAsync(arguments, cancellationToken),
                "health_check" => await HealthCheckAsync(cancellationToken),
                _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return new ToolCallResponse
            {
                IsError = true,
                Content = new List<ToolContent>
                {
                    new ToolContent
                    {
                        Type = "text",
                        Text = $"Error: {ex.Message}"
                    }
                }
            };
        }
    }

    private async Task<ToolCallResponse> GetVatSubmissionsAsync(CancellationToken cancellationToken)
    {
        var submissions = await _apiClient.GetSubmissionsAsync(cancellationToken);
        var json = JsonSerializer.Serialize(submissions, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Retrieved {submissions.Total} VAT submissions:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> GetVatSubmissionAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var submission = await _apiClient.GetSubmissionAsync(redovisare, period, cancellationToken);

        if (submission == null)
        {
            return new ToolCallResponse
            {
                Content = new List<ToolContent>
                {
                    new ToolContent
                    {
                        Type = "text",
                        Text = $"No submission found for {redovisare}/{period}"
                    }
                }
            };
        }

        var json = JsonSerializer.Serialize(submission, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"VAT submission for {redovisare}/{period}:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> GetVatDecisionsAsync(CancellationToken cancellationToken)
    {
        var decisions = await _apiClient.GetDecisionsAsync(cancellationToken);
        var json = JsonSerializer.Serialize(decisions, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Retrieved {decisions.Total} VAT decisions:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> GetVatDecisionAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var decision = await _apiClient.GetDecisionAsync(redovisare, period, cancellationToken);

        if (decision == null)
        {
            return new ToolCallResponse
            {
                Content = new List<ToolContent>
                {
                    new ToolContent
                    {
                        Type = "text",
                        Text = $"No decision found for {redovisare}/{period}"
                    }
                }
            };
        }

        var json = JsonSerializer.Serialize(decision, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"VAT decision for {redovisare}/{period}:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> HealthCheckAsync(CancellationToken cancellationToken)
    {
        var health = await _apiClient.PingAsync(cancellationToken);
        var json = JsonSerializer.Serialize(health, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Health check result:\n{json}"
                }
            }
        };
    }

    private T GetRequiredArgument<T>(Dictionary<string, object>? arguments, string name)
    {
        if (arguments == null || !arguments.ContainsKey(name))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        var value = arguments[name];

        if (value is JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText())
                ?? throw new ArgumentException($"Failed to deserialize argument: {name}");
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
