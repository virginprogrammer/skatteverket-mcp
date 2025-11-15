using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkatteverketMcpServer.Models;
using SkatteverketMcpServer.Services;
using SkatteverketMcpServer.Tools;
using Xunit;

namespace SkatteverketMcpServer.Tests.Tools;

public class VatDraftToolsTests
{
    private readonly Mock<ISkatteverketApiClient> _mockApiClient;
    private readonly Mock<ILogger<VatDraftTools>> _mockLogger;
    private readonly VatDraftTools _tools;

    public VatDraftToolsTests()
    {
        _mockApiClient = new Mock<ISkatteverketApiClient>();
        _mockLogger = new Mock<ILogger<VatDraftTools>>();
        _tools = new VatDraftTools(_mockApiClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void GetToolDefinitions_ShouldReturnAllDraftTools()
    {
        // Act
        var tools = _tools.GetToolDefinitions();

        // Assert
        tools.Should().NotBeEmpty();
        tools.Should().Contain(t => t.Name == "get_vat_drafts");
        tools.Should().Contain(t => t.Name == "get_vat_draft");
        tools.Should().Contain(t => t.Name == "create_vat_draft");
        tools.Should().Contain(t => t.Name == "delete_vat_draft");
        tools.Should().Contain(t => t.Name == "validate_vat_draft");
        tools.Should().Contain(t => t.Name == "lock_vat_draft");
        tools.Should().Contain(t => t.Name == "unlock_vat_draft");
    }

    [Fact]
    public async Task ExecuteToolAsync_GetVatDrafts_ShouldCallApiClientAsync()
    {
        // Arrange
        var expectedDrafts = new VatDraftListResponse
        {
            Drafts = new List<VatDraft>
            {
                new VatDraft
                {
                    Redovisare = "1234567890",
                    Period = "2024-01",
                    Status = "draft"
                }
            },
            Total = 1
        };

        _mockApiClient
            .Setup(x => x.GetDraftsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDrafts);

        // Act
        var result = await _tools.ExecuteToolAsync("get_vat_drafts", null);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content.Should().NotBeEmpty();
        _mockApiClient.Verify(x => x.GetDraftsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_GetVatDraft_WithValidArguments_ShouldReturnDraftAsync()
    {
        // Arrange
        var redovisare = "1234567890";
        var period = "2024-01";
        var expectedDraft = new VatDraft
        {
            Redovisare = redovisare,
            Period = period,
            Status = "draft",
            Momsinkomst = 100000m,
            UtgaendeMoms = 25000m,
            IngaendeMoms = 5000m
        };

        _mockApiClient
            .Setup(x => x.GetDraftAsync(redovisare, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var arguments = new Dictionary<string, object>
        {
            ["redovisare"] = redovisare,
            ["period"] = period
        };

        // Act
        var result = await _tools.ExecuteToolAsync("get_vat_draft", arguments);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain(redovisare);
        result.Content[0].Text.Should().Contain(period);
    }

    [Fact]
    public async Task ExecuteToolAsync_CreateVatDraft_ShouldCallApiClientAsync()
    {
        // Arrange
        var redovisare = "1234567890";
        var period = "2024-01";
        var expectedDraft = new VatDraft
        {
            Redovisare = redovisare,
            Period = period,
            Status = "draft",
            Momsinkomst = 100000m
        };

        _mockApiClient
            .Setup(x => x.CreateOrUpdateDraftAsync(
                redovisare,
                period,
                It.IsAny<VatDraftRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var arguments = new Dictionary<string, object>
        {
            ["redovisare"] = redovisare,
            ["period"] = period,
            ["momsinkomst"] = 100000m
        };

        // Act
        var result = await _tools.ExecuteToolAsync("create_vat_draft", arguments);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        _mockApiClient.Verify(
            x => x.CreateOrUpdateDraftAsync(
                redovisare,
                period,
                It.IsAny<VatDraftRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_ValidateVatDraft_ShouldReturnValidationResultAsync()
    {
        // Arrange
        var redovisare = "1234567890";
        var period = "2024-01";
        var validationResponse = new VatValidationResponse
        {
            Valid = true,
            Errors = new List<ValidationError>()
        };

        _mockApiClient
            .Setup(x => x.ValidateDraftAsync(redovisare, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResponse);

        var arguments = new Dictionary<string, object>
        {
            ["redovisare"] = redovisare,
            ["period"] = period
        };

        // Act
        var result = await _tools.ExecuteToolAsync("validate_vat_draft", arguments);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("valid");
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ShouldReturnErrorAsync()
    {
        // Act
        var result = await _tools.ExecuteToolAsync("unknown_tool", null);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error");
    }
}
