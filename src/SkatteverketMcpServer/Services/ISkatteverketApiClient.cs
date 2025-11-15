using SkatteverketMcpServer.Models;

namespace SkatteverketMcpServer.Services;

/// <summary>
/// Interface for Skatteverket API client
/// </summary>
public interface ISkatteverketApiClient
{
    /// <summary>
    /// Health check ping
    /// </summary>
    Task<HealthResponse> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of drafts
    /// </summary>
    Task<VatDraftListResponse> GetDraftsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific draft
    /// </summary>
    Task<VatDraft?> GetDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update draft
    /// </summary>
    Task<VatDraft> CreateOrUpdateDraftAsync(string redovisare, string period, VatDraftRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete draft
    /// </summary>
    Task<bool> DeleteDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate draft
    /// </summary>
    Task<VatValidationResponse> ValidateDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock draft for signing
    /// </summary>
    Task<bool> LockDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlock draft
    /// </summary>
    Task<bool> UnlockDraftAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get submitted declarations
    /// </summary>
    Task<VatSubmissionListResponse> GetSubmissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific submission
    /// </summary>
    Task<VatSubmission?> GetSubmissionAsync(string redovisare, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get decided declarations
    /// </summary>
    Task<VatDecisionListResponse> GetDecisionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific decision
    /// </summary>
    Task<VatDecision?> GetDecisionAsync(string redovisare, string period, CancellationToken cancellationToken = default);
}
