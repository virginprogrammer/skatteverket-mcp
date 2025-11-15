# Usage Examples

This document provides practical examples of using the Skatteverket MCP Server with various LLM clients.

## Table of Contents
1. [Basic Tool Usage](#basic-tool-usage)
2. [Resource Access](#resource-access)
3. [Prompt Templates](#prompt-templates)
4. [Complete Workflows](#complete-workflows)
5. [Error Handling](#error-handling)

## Basic Tool Usage

### Example 1: Check API Health

**Natural Language Request:**
```
Check if the Skatteverket API is available
```

**MCP Tool Invocation:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "health_check",
    "arguments": {}
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Health check result:\n{\n  \"status\": \"healthy\",\n  \"timestamp\": \"2024-01-15T10:30:00Z\",\n  \"version\": \"1.0.0\"\n}"
      }
    ],
    "isError": false
  }
}
```

### Example 2: Retrieve All Drafts

**Natural Language Request:**
```
Show me all my VAT drafts
```

**MCP Tool Invocation:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_vat_drafts",
    "arguments": {}
  }
}
```

**LLM Response to User:**
```
I found 3 VAT drafts:

1. Company 5567891234, Period 2024-01
   Status: Draft
   Amount to pay: 20,000 SEK

2. Company 5567891234, Period 2024-02
   Status: Draft
   Amount to pay: 15,000 SEK

3. Company 9988776655, Period 2024-01
   Status: Validated
   Amount to receive: 5,000 SEK
```

### Example 3: Create VAT Draft

**Natural Language Request:**
```
Create a VAT declaration for company 5567891234 for February 2024.
Sales: 150,000 SEK, Outgoing VAT: 37,500 SEK, Incoming VAT: 8,000 SEK
```

**MCP Tool Invocation:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "create_vat_draft",
    "arguments": {
      "redovisare": "5567891234",
      "period": "2024-02",
      "momsinkomst": 150000,
      "utgaendeMoms": 37500,
      "ingaendeMoms": 8000
    }
  }
}
```

**LLM Response to User:**
```
I've successfully created a VAT draft for company 5567891234 for February 2024:

📊 Summary:
- Sales subject to VAT: 150,000 SEK
- Outgoing VAT: 37,500 SEK
- Incoming VAT: 8,000 SEK
- Net amount to pay: 29,500 SEK

The draft has been saved. Would you like me to validate it?
```

### Example 4: Validate Draft

**Natural Language Request:**
```
Validate the February 2024 draft for company 5567891234
```

**MCP Tool Invocation:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "validate_vat_draft",
    "arguments": {
      "redovisare": "5567891234",
      "period": "2024-02"
    }
  }
}
```

**Response (Valid):**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Validation result for 5567891234/2024-02:\n{\n  \"valid\": true,\n  \"errors\": [],\n  \"warnings\": []\n}"
      }
    ],
    "isError": false
  }
}
```

**LLM Response to User:**
```
✅ Validation successful!

The draft for February 2024 has been validated with no errors or warnings.
Your draft is ready to be locked for signing.
```

**Response (With Errors):**
```json
{
  "valid": false,
  "errors": [
    {
      "field": "utgaendeMoms",
      "message": "Outgoing VAT does not match 25% of income",
      "code": "VAT_MISMATCH"
    }
  ],
  "warnings": [
    {
      "field": "ingaendeMoms",
      "message": "Incoming VAT is unusually high"
    }
  ]
}
```

**LLM Response to User:**
```
⚠️ Validation found issues:

Errors:
❌ Outgoing VAT does not match 25% of income

Warnings:
⚠️ Incoming VAT is unusually high

Please review these issues before proceeding.
```

## Resource Access

### Example 5: Read Draft as Resource

**Natural Language Request:**
```
Show me the details of the January 2024 draft for company 5567891234
```

**MCP Resource Read:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "vat://drafts/5567891234/2024-01"
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "contents": [
      {
        "uri": "vat://drafts/5567891234/2024-01",
        "mimeType": "application/json",
        "text": "{\n  \"redovisare\": \"5567891234\",\n  \"period\": \"2024-01\",\n  \"momsinkomst\": 100000,\n  \"utgaendeMoms\": 25000,\n  \"ingaendeMoms\": 5000,\n  \"attBetala\": 20000,\n  \"status\": \"draft\",\n  \"skapad\": \"2024-01-05T09:00:00Z\",\n  \"uppdaterad\": \"2024-01-10T14:30:00Z\"\n}"
      }
    ]
  }
}
```

### Example 6: List Available Resources

**MCP Resource List:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "resources/list",
  "params": {}
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "resources": [
      {
        "uri": "vat://status",
        "name": "API Health Status",
        "description": "Current health and status of the Skatteverket API connection",
        "mimeType": "application/json"
      },
      {
        "uri": "vat://drafts/5567891234/2024-01",
        "name": "VAT Draft - 5567891234/2024-01",
        "description": "VAT declaration draft for period 2024-01",
        "mimeType": "application/json"
      },
      {
        "uri": "vat://drafts/5567891234/2024-02",
        "name": "VAT Draft - 5567891234/2024-02",
        "description": "VAT declaration draft for period 2024-02",
        "mimeType": "application/json"
      }
    ]
  }
}
```

## Prompt Templates

### Example 7: Monthly VAT Creation Workflow

**Natural Language Request:**
```
Help me create a monthly VAT declaration for company 5567891234, January 2024
```

**MCP Prompt Get:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "prompts/get",
  "params": {
    "name": "create_monthly_vat",
    "arguments": {
      "redovisare": "5567891234",
      "period": "2024-01"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "I need to create a monthly VAT declaration for:\n- Redovisare: 5567891234\n- Period: 2024-01\n\nPlease guide me through the following steps:\n1. Check if a draft already exists\n2. Gather the required information:\n   - Total sales subject to VAT (momsinkomst)\n   - Outgoing VAT (utgående moms)\n   - Incoming VAT (ingående moms)\n3. Create or update the draft\n4. Validate the draft\n5. Show me a summary\n\nCan you help me with this process?"
        }
      }
    ]
  }
}
```

**LLM Guided Workflow:**
```
I'll help you create the VAT declaration for company 5567891234, January 2024.
Let me guide you through each step:

Step 1: Checking for existing draft...
[Calls get_vat_draft tool]
✓ No existing draft found. We'll create a new one.

Step 2: Please provide the following information:
- Total sales subject to VAT?
  [User: 200,000 SEK]
- Outgoing VAT (25%)?
  [User: 50,000 SEK]
- Incoming VAT from purchases?
  [User: 12,000 SEK]

Step 3: Creating draft...
[Calls create_vat_draft tool]
✓ Draft created successfully

Step 4: Validating...
[Calls validate_vat_draft tool]
✓ Validation passed with no errors

Step 5: Summary:
📊 VAT Declaration - January 2024
   Company: 5567891234

   Sales: 200,000 SEK
   Outgoing VAT: 50,000 SEK
   Incoming VAT: 12,000 SEK

   💰 Net amount to pay: 38,000 SEK

   Status: Draft validated, ready for signing

Would you like to lock this draft for signing?
```

### Example 8: Review Draft Workflow

**Natural Language Request:**
```
Review my VAT draft for company 5567891234, February 2024
```

**MCP Prompt Get:**
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "prompts/get",
  "params": {
    "name": "review_draft",
    "arguments": {
      "redovisare": "5567891234",
      "period": "2024-02"
    }
  }
}
```

**LLM Review Process:**
```
I'll review your VAT draft for company 5567891234, February 2024:

1️⃣ Retrieving draft...
   ✓ Draft found

2️⃣ Validating for errors...
   ✓ No validation errors
   ⚠️ 1 warning found

3️⃣ Checking calculations:
   Sales: 150,000 SEK
   Outgoing VAT (25%): 37,500 SEK ✓ Correct
   Incoming VAT: 8,000 SEK
   Net to pay: 29,500 SEK ✓ Correct

4️⃣ Review Summary:

   ✅ All required fields completed
   ✅ Calculations are correct
   ⚠️ Warning: Incoming VAT is 21% of outgoing VAT (higher than typical)

   The draft is technically valid but you may want to verify your
   incoming VAT amount is correct before submission.

Ready to proceed? Reply 'lock' to lock for signing, or 'edit' to make changes.
```

## Complete Workflows

### Example 9: Full Declaration Workflow

**User Conversation:**

```
User: I need to file my VAT for March 2024