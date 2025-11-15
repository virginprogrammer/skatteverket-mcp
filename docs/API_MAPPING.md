# API Mapping: Skatteverket REST to MCP Tools

This document maps Skatteverket REST API endpoints to MCP server tools, resources, and prompts.

## Tools Mapping

### Draft Operations

| MCP Tool | HTTP Method | REST Endpoint | Description |
|----------|-------------|---------------|-------------|
| `get_vat_drafts` | POST | `/api/utkast` | Retrieve all VAT drafts |
| `get_vat_draft` | GET | `/api/utkast/{redovisare}/{period}` | Get specific draft |
| `create_vat_draft` | POST | `/api/utkast/{redovisare}/{period}` | Create/update draft |
| `delete_vat_draft` | DELETE | `/api/utkast/{redovisare}/{period}` | Delete draft |
| `validate_vat_draft` | POST | `/api/kontrollera/{redovisare}/{period}` | Validate draft |
| `lock_vat_draft` | PUT | `/api/las/{redovisare}/{period}` | Lock for signing |
| `unlock_vat_draft` | DELETE | `/api/las/{redovisare}/{period}` | Unlock draft |

### Submission Operations

| MCP Tool | HTTP Method | REST Endpoint | Description |
|----------|-------------|---------------|-------------|
| `get_vat_submissions` | POST | `/api/inlamnat` | Get all submissions |
| `get_vat_submission` | GET | `/api/inlamnat/{redovisare}/{period}` | Get specific submission |

### Decision Operations

| MCP Tool | HTTP Method | REST Endpoint | Description |
|----------|-------------|---------------|-------------|
| `get_vat_decisions` | POST | `/api/beslutat` | Get all decisions |
| `get_vat_decision` | GET | `/api/beslutat/{redovisare}/{period}` | Get specific decision |

### Health Check

| MCP Tool | HTTP Method | REST Endpoint | Description |
|----------|-------------|---------------|-------------|
| `health_check` | GET | `/api/ping` | API connectivity test |

## Resources Mapping

| MCP Resource URI | Data Source | Description |
|-----------------|-------------|-------------|
| `vat://status` | `/api/ping` | Real-time API health status |
| `vat://drafts/{redovisare}/{period}` | `/api/utkast/{redovisare}/{period}` | Individual draft as resource |
| `vat://submissions/{redovisare}/{period}` | `/api/inlamnat/{redovisare}/{period}` | Submission record as resource |
| `vat://decisions/{redovisare}/{period}` | `/api/beslutat/{redovisare}/{period}` | Decision document as resource |

## Tool Parameters

### get_vat_draft

**Parameters:**
- `redovisare` (string, required): Tax reporter ID (personnummer/organisationsnummer)
- `period` (string, required): Reporting period (format: `YYYY-MM`)

**Example:**
```json
{
  "name": "get_vat_draft",
  "arguments": {
    "redovisare": "5567891234",
    "period": "2024-01"
  }
}
```

**Response:**
```json
{
  "content": [
    {
      "type": "text",
      "text": "VAT draft for 5567891234/2024-01:\n{\n  \"redovisare\": \"5567891234\",\n  \"period\": \"2024-01\",\n  \"momsinkomst\": 100000,\n  \"utgaendeMoms\": 25000,\n  \"ingaendeMoms\": 5000,\n  \"attBetala\": 20000,\n  \"status\": \"draft\"\n}"
    }
  ],
  "isError": false
}
```

### create_vat_draft

**Parameters:**
- `redovisare` (string, required): Tax reporter ID
- `period` (string, required): Reporting period
- `momsinkomst` (number, optional): VAT income amount
- `utgaendeMoms` (number, optional): Outgoing VAT
- `ingaendeMoms` (number, optional): Incoming VAT

**Example:**
```json
{
  "name": "create_vat_draft",
  "arguments": {
    "redovisare": "5567891234",
    "period": "2024-01",
    "momsinkomst": 100000,
    "utgaendeMoms": 25000,
    "ingaendeMoms": 5000
  }
}
```

### validate_vat_draft

**Parameters:**
- `redovisare` (string, required): Tax reporter ID
- `period` (string, required): Reporting period

**Example Response:**
```json
{
  "content": [
    {
      "type": "text",
      "text": "Validation result for 5567891234/2024-01:\n{\n  \"valid\": true,\n  \"errors\": [],\n  \"warnings\": []\n}"
    }
  ],
  "isError": false
}
```

## Prompts Mapping

### create_monthly_vat

**Purpose:** Guided workflow for creating monthly VAT declarations

**Parameters:**
- `redovisare` (string, required)
- `period` (string, required)

**Workflow Steps:**
1. Check for existing draft
2. Gather required information
3. Create/update draft
4. Validate
5. Show summary

**Generated Prompt:**
```
I need to create a monthly VAT declaration for:
- Redovisare: {redovisare}
- Period: {period}

Please guide me through the following steps:
1. Check if a draft already exists
2. Gather the required information...
```

### review_draft

**Purpose:** Review and validate a VAT draft before submission

**Parameters:**
- `redovisare` (string, required)
- `period` (string, required)

**Workflow Steps:**
1. Retrieve current draft
2. Validate for errors
3. Check calculations
4. Provide summary

### check_status

**Purpose:** Check status of VAT declarations

**Parameters:**
- `redovisare` (string, optional)

**Workflow Steps:**
1. Show all drafts
2. Show recent submissions
3. Show pending decisions
4. Highlight deadlines

### submission_checklist

**Purpose:** Pre-submission verification checklist

**Parameters:**
- `redovisare` (string, required)
- `period` (string, required)

**Checklist Items:**
- Draft exists and is complete
- All required fields filled
- Validation passes
- Calculations correct
- Supporting docs ready
- Draft locked for signing

## Error Handling

### JSON-RPC Error Codes

| Code | Name | Description |
|------|------|-------------|
| -32700 | Parse error | Invalid JSON |
| -32600 | Invalid Request | Invalid request object |
| -32601 | Method not found | Unknown method |
| -32602 | Invalid params | Invalid parameters |
| -32603 | Internal error | Server error |
| -32000 | MCP error | Generic MCP error |
| -32001 | Tool execution error | Tool failed |
| -32002 | Resource not found | Resource doesn't exist |
| -32003 | Prompt not found | Prompt doesn't exist |

### Example Error Response

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32602,
    "message": "Missing required argument: redovisare"
  }
}
```

## Data Models

### VatDraft

```json
{
  "redovisare": "string",
  "period": "string",
  "momsinkomst": 0,
  "utgaendeMoms": 0,
  "ingaendeMoms": 0,
  "attBetala": 0,
  "attFaaTillbaka": 0,
  "status": "string",
  "skapad": "2024-01-15T10:30:00Z",
  "uppdaterad": "2024-01-15T14:20:00Z"
}
```

### VatSubmission

```json
{
  "redovisare": "string",
  "period": "string",
  "inlamningsdatum": "2024-01-20T09:00:00Z",
  "status": "submitted",
  "kvittonummer": "ABC123456",
  "belopp": 20000
}
```

### VatDecision

```json
{
  "redovisare": "string",
  "period": "string",
  "beslutsdatum": "2024-02-01T10:00:00Z",
  "status": "approved",
  "belopp": 20000,
  "beskrivning": "Beslut godkänt"
}
```

## Authentication Flow

### OAuth 2.0 + Certificate

1. **Token Request:**
```http
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

client_id={client_id}&
client_secret={client_secret}&
grant_type=client_credentials&
scope={scopes}
```

2. **API Request with Token:**
```http
GET /api/utkast/{redovisare}/{period}
Authorization: Bearer {access_token}
X-Client-Certificate: {certificate}
```

3. **Token Refresh:**
- Tokens are cached and automatically refreshed
- Retry logic handles token expiration
- Certificate validation on each request

## Rate Limiting

The Skatteverket API implements rate limiting:

- **Rate Limit:** 100 requests per minute
- **Burst Limit:** 20 requests per second
- **Headers:**
  - `X-RateLimit-Limit`: Total allowed
  - `X-RateLimit-Remaining`: Remaining requests
  - `X-RateLimit-Reset`: Reset timestamp

The MCP server implements:
- Exponential backoff on rate limit errors
- Request queuing
- Automatic retry with delays

## Period Format

VAT periods use the format `YYYY-MM`:

**Examples:**
- Monthly: `2024-01`, `2024-02`, etc.
- Quarterly: `2024-Q1`, `2024-Q2`, etc.
- Annual: `2024`

**Validation:**
- Must be valid date format
- Cannot be in the future
- Must align with company's reporting schedule

## Testing

### Mock Endpoints for Development

Set `Skatteverket:BaseUrl` to `https://api-test.skatteverket.se` for testing.

Test credentials are available through Skatteverket's developer portal.

### Example Test Scenario

```bash
# 1. Create draft
curl -X POST http://localhost:5000/tools/call \
  -H "Content-Type: application/json" \
  -d '{"name":"create_vat_draft","arguments":{"redovisare":"test123","period":"2024-01"}}'

# 2. Validate draft
curl -X POST http://localhost:5000/tools/call \
  -H "Content-Type: application/json" \
  -d '{"name":"validate_vat_draft","arguments":{"redovisare":"test123","period":"2024-01"}}'

# 3. Get draft resource
curl -X POST http://localhost:5000/resources/read \
  -H "Content-Type: application/json" \
  -d '{"uri":"vat://drafts/test123/2024-01"}'
```
