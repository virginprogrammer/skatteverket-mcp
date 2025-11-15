using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkatteverketMcpServer.Prompts;
using Xunit;

namespace SkatteverketMcpServer.Tests.Prompts;

public class VatPromptsTests
{
    private readonly Mock<ILogger<VatPrompts>> _mockLogger;
    private readonly VatPrompts _prompts;

    public VatPromptsTests()
    {
        _mockLogger = new Mock<ILogger<VatPrompts>>();
        _prompts = new VatPrompts(_mockLogger.Object);
    }

    [Fact]
    public void GetPromptDefinitions_ShouldReturnAllPrompts()
    {
        // Act
        var prompts = _prompts.GetPromptDefinitions();

        // Assert
        prompts.Should().NotBeEmpty();
        prompts.Should().Contain(p => p.Name == "create_monthly_vat");
        prompts.Should().Contain(p => p.Name == "review_draft");
        prompts.Should().Contain(p => p.Name == "check_status");
        prompts.Should().Contain(p => p.Name == "submission_checklist");
    }

    [Fact]
    public void GetPromptMessages_CreateMonthlyVat_ShouldReturnMessages()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["redovisare"] = "1234567890",
            ["period"] = "2024-01"
        };

        // Act
        var messages = _prompts.GetPromptMessages("create_monthly_vat", arguments);

        // Assert
        messages.Should().NotBeEmpty();
        messages[0].Role.Should().Be("user");
        messages[0].Content.Text.Should().Contain("1234567890");
        messages[0].Content.Text.Should().Contain("2024-01");
    }

    [Fact]
    public void GetPromptMessages_ReviewDraft_ShouldReturnMessages()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["redovisare"] = "1234567890",
            ["period"] = "2024-01"
        };

        // Act
        var messages = _prompts.GetPromptMessages("review_draft", arguments);

        // Assert
        messages.Should().NotBeEmpty();
        messages[0].Content.Text.Should().Contain("review");
        messages[0].Content.Text.Should().Contain("validate");
    }

    [Fact]
    public void GetPromptMessages_UnknownPrompt_ShouldThrowException()
    {
        // Act
        Action act = () => _prompts.GetPromptMessages("unknown_prompt", null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown_prompt*");
    }
}
