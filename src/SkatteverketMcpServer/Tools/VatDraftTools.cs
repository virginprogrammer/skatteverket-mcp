using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;
using SkatteverketMcpServer.Services;

namespace SkatteverketMcpServer.Tools;

/// <summary>
/// MCP Tools for VAT draft operations
/// </summary>
public class VatDraftTools
{
    private readonly ISkatteverketApiClient _apiClient;
    private readonly ILogger<VatDraftTools> _logger;

    public VatDraftTools(ISkatteverketApiClient apiClient, ILogger<VatDraftTools> logger)
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
                Name = "get_vat_drafts",
                Description = "Retrieve all VAT declaration drafts for the authenticated user",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            },
            new McpTool
            {
                Name = "get_vat_draft",
                Description = "Get a specific VAT declaration draft by redovisare (reporter ID) and period",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["redovisare"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The tax reporter ID (personnummer/organisationsnummer)"
                        },
                        ["period"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The reporting period (e.g., '2024-01' for January 2024)"
                        }
                    },
                    Required = new List<string> { "redovisare", "period" }
                }
            },
            new McpTool
            {
                Name = "create_vat_draft",
                Description = "Create or update a VAT declaration draft with financial data",
                InputSchema = new ToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["redovisare"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The tax reporter ID (personnummer/organisationsnummer)"
                        },
                        ["period"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "The reporting period (e.g., '2024-01' for January 2024)"
                        },
                        ["momsinkomst"] = new SchemaProperty
                        {
                            Type = "number",
                            Description = "VAT income amount (optional)"
                        },
                        ["utgaendeMoms"] = new SchemaProperty
                        {
                            Type = "number",
                            Description = "Outgoing VAT amount (optional)"
                        },
                        ["ingaendeMoms"] = new SchemaProperty
                        {
                            Type = "number",
                            Description = "Incoming VAT amount (optional)"
                        }
                    },
                    Required = new List<string> { "redovisare", "period" }
                }
            },
            new McpTool
            {
                Name = "delete_vat_draft",
                Description = "Delete a VAT declaration draft",
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
                Name = "validate_vat_draft",
                Description = "Validate a VAT declaration draft and check for errors",
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
                Name = "lock_vat_draft",
                Description = "Lock a VAT draft for signing (prevents further modifications)",
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
                Name = "unlock_vat_draft",
                Description = "Unlock a VAT draft to allow modifications",
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
                "get_vat_drafts" => await GetVatDraftsAsync(cancellationToken),
                "get_vat_draft" => await GetVatDraftAsync(arguments, cancellationToken),
                "create_vat_draft" => await CreateVatDraftAsync(arguments, cancellationToken),
                "delete_vat_draft" => await DeleteVatDraftAsync(arguments, cancellationToken),
                "validate_vat_draft" => await ValidateVatDraftAsync(arguments, cancellationToken),
                "lock_vat_draft" => await LockVatDraftAsync(arguments, cancellationToken),
                "unlock_vat_draft" => await UnlockVatDraftAsync(arguments, cancellationToken),
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

    private async Task<ToolCallResponse> GetVatDraftsAsync(CancellationToken cancellationToken)
    {
        var drafts = await _apiClient.GetDraftsAsync(cancellationToken);
        var json = JsonSerializer.Serialize(drafts, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Retrieved {drafts.Total} VAT drafts:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> GetVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var draft = await _apiClient.GetDraftAsync(redovisare, period, cancellationToken);

        if (draft == null)
        {
            return new ToolCallResponse
            {
                Content = new List<ToolContent>
                {
                    new ToolContent
                    {
                        Type = "text",
                        Text = $"No draft found for {redovisare}/{period}"
                    }
                }
            };
        }

        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"VAT draft for {redovisare}/{period}:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> CreateVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var request = new VatDraftRequest
        {
            Momsinkomst = GetOptionalArgument<decimal?>(arguments, "momsinkomst"),
            UtgaendeMoms = GetOptionalArgument<decimal?>(arguments, "utgaendeMoms"),
            IngaendeMoms = GetOptionalArgument<decimal?>(arguments, "ingaendeMoms")
        };

        var draft = await _apiClient.CreateOrUpdateDraftAsync(redovisare, period, request, cancellationToken);
        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Successfully created/updated VAT draft:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> DeleteVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var deleted = await _apiClient.DeleteDraftAsync(redovisare, period, cancellationToken);

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = deleted
                        ? $"Successfully deleted VAT draft for {redovisare}/{period}"
                        : $"No draft found for {redovisare}/{period}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> ValidateVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        var validation = await _apiClient.ValidateDraftAsync(redovisare, period, cancellationToken);
        var json = JsonSerializer.Serialize(validation, new JsonSerializerOptions { WriteIndented = true });

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Validation result for {redovisare}/{period}:\n{json}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> LockVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        await _apiClient.LockDraftAsync(redovisare, period, cancellationToken);

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Successfully locked VAT draft for {redovisare}/{period}"
                }
            }
        };
    }

    private async Task<ToolCallResponse> UnlockVatDraftAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var redovisare = GetRequiredArgument<string>(arguments, "redovisare");
        var period = GetRequiredArgument<string>(arguments, "period");

        await _apiClient.UnlockDraftAsync(redovisare, period, cancellationToken);

        return new ToolCallResponse
        {
            Content = new List<ToolContent>
            {
                new ToolContent
                {
                    Type = "text",
                    Text = $"Successfully unlocked VAT draft for {redovisare}/{period}"
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

        // Handle JsonElement for deserialized JSON
        if (value is JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText())
                ?? throw new ArgumentException($"Failed to deserialize argument: {name}");
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private T? GetOptionalArgument<T>(Dictionary<string, object>? arguments, string name)
    {
        if (arguments == null || !arguments.ContainsKey(name))
        {
            return default;
        }

        var value = arguments[name];

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        }

        return (T?)Convert.ChangeType(value, typeof(T));
    }
}
