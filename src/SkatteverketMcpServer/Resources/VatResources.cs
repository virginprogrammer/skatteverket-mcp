using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;
using SkatteverketMcpServer.Services;

namespace SkatteverketMcpServer.Resources;

/// <summary>
/// MCP Resources for VAT data exposure
/// </summary>
public class VatResources
{
    private readonly ISkatteverketApiClient _apiClient;
    private readonly ILogger<VatResources> _logger;

    public VatResources(ISkatteverketApiClient apiClient, ILogger<VatResources> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all resource definitions
    /// </summary>
    public async Task<List<McpResource>> GetResourceListAsync(CancellationToken cancellationToken = default)
    {
        var resources = new List<McpResource>
        {
            new McpResource
            {
                Uri = "vat://status",
                Name = "API Health Status",
                Description = "Current health and status of the Skatteverket API connection",
                MimeType = "application/json"
            }
        };

        // Dynamically add resources for existing drafts
        try
        {
            var drafts = await _apiClient.GetDraftsAsync(cancellationToken);
            foreach (var draft in drafts.Drafts)
            {
                resources.Add(new McpResource
                {
                    Uri = $"vat://drafts/{draft.Redovisare}/{draft.Period}",
                    Name = $"VAT Draft - {draft.Redovisare}/{draft.Period}",
                    Description = $"VAT declaration draft for period {draft.Period}",
                    MimeType = "application/json"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load draft resources");
        }

        return resources;
    }

    /// <summary>
    /// Read a specific resource by URI
    /// </summary>
    public async Task<McpResourceContent> ReadResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading resource: {Uri}", uri);

        if (uri == "vat://status")
        {
            return await ReadStatusResourceAsync(cancellationToken);
        }

        if (uri.StartsWith("vat://drafts/"))
        {
            return await ReadDraftResourceAsync(uri, cancellationToken);
        }

        if (uri.StartsWith("vat://submissions/"))
        {
            return await ReadSubmissionResourceAsync(uri, cancellationToken);
        }

        if (uri.StartsWith("vat://decisions/"))
        {
            return await ReadDecisionResourceAsync(uri, cancellationToken);
        }

        throw new InvalidOperationException($"Unknown resource URI: {uri}");
    }

    private async Task<McpResourceContent> ReadStatusResourceAsync(CancellationToken cancellationToken)
    {
        var health = await _apiClient.PingAsync(cancellationToken);
        var json = JsonSerializer.Serialize(health, new JsonSerializerOptions { WriteIndented = true });

        return new McpResourceContent
        {
            Uri = "vat://status",
            MimeType = "application/json",
            Text = json
        };
    }

    private async Task<McpResourceContent> ReadDraftResourceAsync(string uri, CancellationToken cancellationToken)
    {
        // Parse URI: vat://drafts/{redovisare}/{period}
        var parts = uri.Replace("vat://drafts/", "").Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid draft URI format: {uri}");
        }

        var redovisare = parts[0];
        var period = parts[1];

        var draft = await _apiClient.GetDraftAsync(redovisare, period, cancellationToken);
        if (draft == null)
        {
            throw new InvalidOperationException($"Draft not found: {redovisare}/{period}");
        }

        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });

        return new McpResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    private async Task<McpResourceContent> ReadSubmissionResourceAsync(string uri, CancellationToken cancellationToken)
    {
        // Parse URI: vat://submissions/{redovisare}/{period}
        var parts = uri.Replace("vat://submissions/", "").Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid submission URI format: {uri}");
        }

        var redovisare = parts[0];
        var period = parts[1];

        var submission = await _apiClient.GetSubmissionAsync(redovisare, period, cancellationToken);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission not found: {redovisare}/{period}");
        }

        var json = JsonSerializer.Serialize(submission, new JsonSerializerOptions { WriteIndented = true });

        return new McpResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    private async Task<McpResourceContent> ReadDecisionResourceAsync(string uri, CancellationToken cancellationToken)
    {
        // Parse URI: vat://decisions/{redovisare}/{period}
        var parts = uri.Replace("vat://decisions/", "").Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid decision URI format: {uri}");
        }

        var redovisare = parts[0];
        var period = parts[1];

        var decision = await _apiClient.GetDecisionAsync(redovisare, period, cancellationToken);
        if (decision == null)
        {
            throw new InvalidOperationException($"Decision not found: {redovisare}/{period}");
        }

        var json = JsonSerializer.Serialize(decision, new JsonSerializerOptions { WriteIndented = true });

        return new McpResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }
}
