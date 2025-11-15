using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;

namespace SkatteverketMcpServer.Services;

/// <summary>
/// HTTP client for Skatteverket API
/// </summary>
public class SkatteverketApiClient : ISkatteverketApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SkatteverketApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SkatteverketApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SkatteverketApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure base URL from configuration
        var baseUrl = configuration["Skatteverket:BaseUrl"] ?? "https://api.skatteverket.se";
        _httpClient.BaseAddress = new Uri(baseUrl);

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // TODO: Add authentication headers (OAuth2, certificates, etc.)
        // This will be configured based on actual API requirements
    }

    public async Task<HealthResponse> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Pinging Skatteverket API");
            var response = await _httpClient.GetAsync("/api/ping", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HealthResponse>(_jsonOptions, cancellationToken);
            return result ?? new HealthResponse { Status = "unknown", Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ping Skatteverket API");
            throw new InvalidOperationException("Failed to connect to Skatteverket API", ex);
        }
    }

    public async Task<VatDraftListResponse> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT drafts");
            var response = await _httpClient.PostAsync("/api/utkast", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VatDraftListResponse>(_jsonOptions, cancellationToken);
            return result ?? new VatDraftListResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT drafts");
            throw new InvalidOperationException("Failed to retrieve VAT drafts", ex);
        }
    }

    public async Task<VatDraft?> GetDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT draft for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.GetAsync($"/api/utkast/{redovisare}/{period}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VatDraft>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to retrieve VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<VatDraft> CreateOrUpdateDraftAsync(string redovisare, string period, VatDraftRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating/updating VAT draft for {Redovisare}/{Period}", redovisare, period);
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"/api/utkast/{redovisare}/{period}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VatDraft>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/update VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to create/update VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<bool> DeleteDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting VAT draft for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.DeleteAsync($"/api/utkast/{redovisare}/{period}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to delete VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<VatValidationResponse> ValidateDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating VAT draft for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.PostAsync($"/api/kontrollera/{redovisare}/{period}", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VatValidationResponse>(_jsonOptions, cancellationToken);
            return result ?? new VatValidationResponse { Valid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to validate VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<bool> LockDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Locking VAT draft for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.PutAsync($"/api/las/{redovisare}/{period}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to lock VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<bool> UnlockDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Unlocking VAT draft for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.DeleteAsync($"/api/las/{redovisare}/{period}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock VAT draft for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to unlock VAT draft for {redovisare}/{period}", ex);
        }
    }

    public async Task<VatSubmissionListResponse> GetSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT submissions");
            var response = await _httpClient.PostAsync("/api/inlamnat", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VatSubmissionListResponse>(_jsonOptions, cancellationToken);
            return result ?? new VatSubmissionListResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT submissions");
            throw new InvalidOperationException("Failed to retrieve VAT submissions", ex);
        }
    }

    public async Task<VatSubmission?> GetSubmissionAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT submission for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.GetAsync($"/api/inlamnat/{redovisare}/{period}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VatSubmission>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT submission for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to retrieve VAT submission for {redovisare}/{period}", ex);
        }
    }

    public async Task<VatDecisionListResponse> GetDecisionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT decisions");
            var response = await _httpClient.PostAsync("/api/beslutat", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VatDecisionListResponse>(_jsonOptions, cancellationToken);
            return result ?? new VatDecisionListResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT decisions");
            throw new InvalidOperationException("Failed to retrieve VAT decisions", ex);
        }
    }

    public async Task<VatDecision?> GetDecisionAsync(string redovisare, string period, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting VAT decision for {Redovisare}/{Period}", redovisare, period);
            var response = await _httpClient.GetAsync($"/api/beslutat/{redovisare}/{period}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VatDecision>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VAT decision for {Redovisare}/{Period}", redovisare, period);
            throw new InvalidOperationException($"Failed to retrieve VAT decision for {redovisare}/{period}", ex);
        }
    }
}
