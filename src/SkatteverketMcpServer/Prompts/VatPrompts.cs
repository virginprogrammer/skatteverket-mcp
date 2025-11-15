using Microsoft.Extensions.Logging;
using SkatteverketMcpServer.Models;

namespace SkatteverketMcpServer.Prompts;

/// <summary>
/// MCP Prompts for common VAT workflows
/// </summary>
public class VatPrompts
{
    private readonly ILogger<VatPrompts> _logger;

    public VatPrompts(ILogger<VatPrompts> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all prompt definitions
    /// </summary>
    public List<McpPrompt> GetPromptDefinitions()
    {
        return new List<McpPrompt>
        {
            new McpPrompt
            {
                Name = "create_monthly_vat",
                Description = "Guided workflow to create a monthly VAT declaration",
                Arguments = new List<PromptArgument>
                {
                    new PromptArgument
                    {
                        Name = "redovisare",
                        Description = "Tax reporter ID (personnummer/organisationsnummer)",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "period",
                        Description = "Reporting period (e.g., '2024-01')",
                        Required = true
                    }
                }
            },
            new McpPrompt
            {
                Name = "review_draft",
                Description = "Review and validate a VAT draft before submission",
                Arguments = new List<PromptArgument>
                {
                    new PromptArgument
                    {
                        Name = "redovisare",
                        Description = "Tax reporter ID",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "period",
                        Description = "Reporting period",
                        Required = true
                    }
                }
            },
            new McpPrompt
            {
                Name = "check_status",
                Description = "Check the status of VAT declarations and submissions",
                Arguments = new List<PromptArgument>
                {
                    new PromptArgument
                    {
                        Name = "redovisare",
                        Description = "Tax reporter ID (optional - shows all if not provided)",
                        Required = false
                    }
                }
            },
            new McpPrompt
            {
                Name = "submission_checklist",
                Description = "Pre-submission checklist for VAT declaration",
                Arguments = new List<PromptArgument>
                {
                    new PromptArgument
                    {
                        Name = "redovisare",
                        Description = "Tax reporter ID",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "period",
                        Description = "Reporting period",
                        Required = true
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get prompt messages by name
    /// </summary>
    public List<McpPromptMessage> GetPromptMessages(string promptName, Dictionary<string, object>? arguments)
    {
        _logger.LogInformation("Getting prompt messages for: {PromptName}", promptName);

        return promptName switch
        {
            "create_monthly_vat" => GetCreateMonthlyVatPrompt(arguments),
            "review_draft" => GetReviewDraftPrompt(arguments),
            "check_status" => GetCheckStatusPrompt(arguments),
            "submission_checklist" => GetSubmissionChecklistPrompt(arguments),
            _ => throw new InvalidOperationException($"Unknown prompt: {promptName}")
        };
    }

    private List<McpPromptMessage> GetCreateMonthlyVatPrompt(Dictionary<string, object>? arguments)
    {
        var redovisare = GetArgument<string>(arguments, "redovisare");
        var period = GetArgument<string>(arguments, "period");

        return new List<McpPromptMessage>
        {
            new McpPromptMessage
            {
                Role = "user",
                Content = new MessageContent
                {
                    Type = "text",
                    Text = $@"I need to create a monthly VAT declaration for:
- Redovisare: {redovisare}
- Period: {period}

Please guide me through the following steps:
1. Check if a draft already exists
2. Gather the required information:
   - Total sales subject to VAT (momsinkomst)
   - Outgoing VAT (utgående moms)
   - Incoming VAT (ingående moms)
3. Create or update the draft
4. Validate the draft
5. Show me a summary

Can you help me with this process?"
                }
            }
        };
    }

    private List<McpPromptMessage> GetReviewDraftPrompt(Dictionary<string, object>? arguments)
    {
        var redovisare = GetArgument<string>(arguments, "redovisare");
        var period = GetArgument<string>(arguments, "period");

        return new List<McpPromptMessage>
        {
            new McpPromptMessage
            {
                Role = "user",
                Content = new MessageContent
                {
                    Type = "text",
                    Text = $@"Please review my VAT draft for {redovisare}/{period}:

1. Retrieve the current draft
2. Validate it for errors
3. Check the calculations:
   - Are the VAT amounts correct?
   - Is the net amount to pay/receive calculated correctly?
4. Provide a summary of any issues or confirm it's ready for submission

Please conduct this review and let me know if anything needs attention."
                }
            }
        };
    }

    private List<McpPromptMessage> GetCheckStatusPrompt(Dictionary<string, object>? arguments)
    {
        var redovisare = arguments?.ContainsKey("redovisare") == true
            ? GetArgument<string>(arguments, "redovisare")
            : "all reporters";

        return new List<McpPromptMessage>
        {
            new McpPromptMessage
            {
                Role = "user",
                Content = new MessageContent
                {
                    Type = "text",
                    Text = $@"Please check the status of my VAT declarations for {redovisare}:

1. Show me all current drafts
2. Show me recent submissions
3. Show me any pending decisions
4. Highlight any deadlines or actions needed

Provide a clear overview of my VAT declaration status."
                }
            }
        };
    }

    private List<McpPromptMessage> GetSubmissionChecklistPrompt(Dictionary<string, object>? arguments)
    {
        var redovisare = GetArgument<string>(arguments, "redovisare");
        var period = GetArgument<string>(arguments, "period");

        return new List<McpPromptMessage>
        {
            new McpPromptMessage
            {
                Role = "user",
                Content = new MessageContent
                {
                    Type = "text",
                    Text = $@"I'm preparing to submit my VAT declaration for {redovisare}/{period}.

Please help me with this pre-submission checklist:

✓ Draft exists and is complete
✓ All required fields are filled
✓ Validation passes without errors
✓ Calculations are correct:
  - Outgoing VAT matches sales
  - Incoming VAT is properly documented
  - Net amount is calculated correctly
✓ Supporting documentation is ready
✓ Draft is locked and ready for signing

Please verify each item and let me know if I'm ready to submit or if anything needs attention."
                }
            }
        };
    }

    private T GetArgument<T>(Dictionary<string, object>? arguments, string name)
    {
        if (arguments == null || !arguments.ContainsKey(name))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return (T)Convert.ChangeType(arguments[name], typeof(T));
    }
}
