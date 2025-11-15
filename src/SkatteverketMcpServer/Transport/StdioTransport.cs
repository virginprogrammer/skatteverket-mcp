using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SkatteverketMcpServer.Transport;

/// <summary>
/// Stdio-based JSON-RPC transport for MCP communication
/// </summary>
public class StdioTransport : IDisposable
{
    private readonly ILogger<StdioTransport> _logger;
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public StdioTransport(ILogger<StdioTransport> logger)
    {
        _logger = logger;
        _inputStream = Console.OpenStandardInput();
        _outputStream = Console.OpenStandardOutput();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Redirect console output to stderr to avoid polluting JSON-RPC communication
        Console.SetOut(Console.Error);
    }

    /// <summary>
    /// Read a JSON-RPC message from stdin
    /// </summary>
    public async Task<JsonDocument?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var buffer = new List<byte>();
            var readBuffer = new byte[1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await _inputStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

                if (bytesRead == 0)
                {
                    // End of stream
                    return null;
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    buffer.Add(readBuffer[i]);

                    // Check for newline delimiter
                    if (readBuffer[i] == '\n')
                    {
                        var json = Encoding.UTF8.GetString(buffer.ToArray()).Trim();
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _logger.LogTrace("Received message: {Message}", json);
                            return JsonDocument.Parse(json);
                        }
                        buffer.Clear();
                    }
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Read operation cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading message from stdin");
            throw;
        }
    }

    /// <summary>
    /// Write a JSON-RPC message to stdout
    /// </summary>
    public async Task WriteMessageAsync(object message, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            _logger.LogTrace("Sending message: {Message}", json);

            await _outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await _outputStream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing message to stdout");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Send a JSON-RPC response
    /// </summary>
    public async Task SendResponseAsync(object id, object? result, CancellationToken cancellationToken = default)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result
        };

        await WriteMessageAsync(response, cancellationToken);
    }

    /// <summary>
    /// Send a JSON-RPC error response
    /// </summary>
    public async Task SendErrorAsync(object? id, int code, string message, object? data = null, CancellationToken cancellationToken = default)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message,
                data
            }
        };

        await WriteMessageAsync(response, cancellationToken);
    }

    /// <summary>
    /// Send a JSON-RPC notification
    /// </summary>
    public async Task SendNotificationAsync(string method, object? @params = null, CancellationToken cancellationToken = default)
    {
        var notification = new
        {
            jsonrpc = "2.0",
            method,
            @params
        };

        await WriteMessageAsync(notification, cancellationToken);
    }

    public void Dispose()
    {
        _writeLock?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// JSON-RPC error codes
/// </summary>
public static class JsonRpcErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    // MCP-specific error codes
    public const int McpError = -32000;
    public const int ToolExecutionError = -32001;
    public const int ResourceNotFound = -32002;
    public const int PromptNotFound = -32003;
}
