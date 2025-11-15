using System.Text.Json.Serialization;

namespace SkatteverketMcpServer.Models;

/// <summary>
/// Represents a VAT declaration draft
/// </summary>
public class VatDraft
{
    [JsonPropertyName("redovisare")]
    public string Redovisare { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("momsinkomst")]
    public decimal? Momsinkomst { get; set; }

    [JsonPropertyName("utgaendeMoms")]
    public decimal? UtgaendeMoms { get; set; }

    [JsonPropertyName("ingaendeMoms")]
    public decimal? IngaendeMoms { get; set; }

    [JsonPropertyName("attBetala")]
    public decimal? AttBetala { get; set; }

    [JsonPropertyName("attFaaTillbaka")]
    public decimal? AttFaaTillbaka { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("skapad")]
    public DateTime? Skapad { get; set; }

    [JsonPropertyName("uppdaterad")]
    public DateTime? Uppdaterad { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request to create or update a VAT draft
/// </summary>
public class VatDraftRequest
{
    [JsonPropertyName("momsinkomst")]
    public decimal? Momsinkomst { get; set; }

    [JsonPropertyName("utgaendeMoms")]
    public decimal? UtgaendeMoms { get; set; }

    [JsonPropertyName("ingaendeMoms")]
    public decimal? IngaendeMoms { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response from draft validation
/// </summary>
public class VatValidationResponse
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("errors")]
    public List<ValidationError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<ValidationWarning>? Warnings { get; set; }
}

public class ValidationError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public class ValidationWarning
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// List of drafts response
/// </summary>
public class VatDraftListResponse
{
    [JsonPropertyName("drafts")]
    public List<VatDraft> Drafts { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

/// <summary>
/// Submitted declaration
/// </summary>
public class VatSubmission
{
    [JsonPropertyName("redovisare")]
    public string Redovisare { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("inlamningsdatum")]
    public DateTime? Inlamningsdatum { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("kvittonummer")]
    public string? Kvittonummer { get; set; }

    [JsonPropertyName("belopp")]
    public decimal? Belopp { get; set; }
}

/// <summary>
/// List of submissions
/// </summary>
public class VatSubmissionListResponse
{
    [JsonPropertyName("submissions")]
    public List<VatSubmission> Submissions { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

/// <summary>
/// Tax decision
/// </summary>
public class VatDecision
{
    [JsonPropertyName("redovisare")]
    public string Redovisare { get; set; } = string.Empty;

    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("beslutsdatum")]
    public DateTime? Beslutsdatum { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("belopp")]
    public decimal? Belopp { get; set; }

    [JsonPropertyName("beskrivning")]
    public string? Beskrivning { get; set; }
}

/// <summary>
/// List of decisions
/// </summary>
public class VatDecisionListResponse
{
    [JsonPropertyName("decisions")]
    public List<VatDecision> Decisions { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

/// <summary>
/// Health check response
/// </summary>
public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
