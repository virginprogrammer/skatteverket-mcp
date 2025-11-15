# Skatteverket MCP Server

[![CI/CD](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml)
[![PR Checks](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/pr-checks.yml/badge.svg)](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/pr-checks.yml)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A Model Context Protocol (MCP) server implementation in C# that exposes Swedish Tax Authority (Skatteverket) VAT declaration functionality to LLM applications via JSON-RPC 2.0.

## Overview

This MCP server acts as a bridge between AI assistants and the Skatteverket VAT declaration API, enabling natural language interactions with VAT declaration workflows.

## Features

### MCP Primitives

#### Tools (12 executable actions)
- **Draft Management**
  - `get_vat_drafts` - Retrieve all VAT declaration drafts
  - `get_vat_draft` - Get a specific draft by ID and period
  - `create_vat_draft` - Create or update a VAT draft
  - `delete_vat_draft` - Remove a draft
  - `validate_vat_draft` - Validate draft for errors
  - `lock_vat_draft` - Lock draft for signing
  - `unlock_vat_draft` - Unlock draft for editing

- **Submissions & Decisions**
  - `get_vat_submissions` - Retrieve submitted declarations
  - `get_vat_submission` - Get specific submission details
  - `get_vat_decisions` - Retrieve tax decisions
  - `get_vat_decision` - Get specific decision details
  - `health_check` - Check API connectivity

#### Resources (Dynamic data exposure)
- `vat://status` - API health status
- `vat://drafts/{redovisare}/{period}` - Individual draft data
- `vat://submissions/{redovisare}/{period}` - Submission records
- `vat://decisions/{redovisare}/{period}` - Decision documents

#### Prompts (Guided workflows)
- `create_monthly_vat` - Guided monthly VAT declaration
- `review_draft` - Draft review workflow
- `check_status` - Status inquiry template
- `submission_checklist` - Pre-submission verification

## Architecture

```
┌─────────────────┐
│   LLM Client    │ (Claude, GPT, etc.)
└────────┬────────┘
         │ JSON-RPC 2.0 / stdio
         │
┌────────▼────────┐
│   MCP Server    │
│  ┌───────────┐  │
│  │  Tools    │  │ (VAT operations)
│  ├───────────┤  │
│  │ Resources │  │ (Data exposure)
│  ├───────────┤  │
│  │  Prompts  │  │ (Workflows)
│  └───────────┘  │
└────────┬────────┘
         │ HTTPS / REST
         │
┌────────▼────────┐
│  Skatteverket   │
│    REST API     │
└─────────────────┘
```

## Prerequisites

- .NET 8.0 SDK or later
- Access to Skatteverket API (credentials required)
- MCP-compatible client (e.g., Claude Desktop, Continue.dev)

## Installation

### 1. Clone and Build

```bash
git clone https://github.com/virginprogrammer/skatteverket-mcp.git
cd skatteverket-mcp
dotnet restore
dotnet build
```

### 2. Configure API Credentials

#### Option A: User Secrets (Development)
```bash
cd src/SkatteverketMcpServer
dotnet user-secrets init
dotnet user-secrets set "Skatteverket:BaseUrl" "https://api-test.skatteverket.se"
dotnet user-secrets set "Authentication:OAuth:ClientId" "your-client-id"
# Add other credentials as needed
```

#### Option B: Environment Variables
```bash
export Skatteverket__BaseUrl="https://api-test.skatteverket.se"
export Authentication__OAuth__ClientId="your-client-id"
```

#### Option C: appsettings.json (Not recommended for production)
Edit `src/SkatteverketMcpServer/appsettings.json` with your credentials.

### 3. Configure MCP Client

#### Claude Desktop Configuration
Edit `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or equivalent:

```json
{
  "mcpServers": {
    "skatteverket": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/skatteverket-mcp/src/SkatteverketMcpServer"
      ],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### VS Code Continue.dev Configuration
Edit `.continue/config.json`:

```json
{
  "mcpServers": [
    {
      "name": "skatteverket",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/skatteverket-mcp/src/SkatteverketMcpServer"
      ]
    }
  ]
}
```

## Usage Examples

### Example 1: Creating a VAT Draft

**User prompt to LLM:**
```
Create a VAT draft for company 5567891234 for January 2024 with:
- Sales subject to VAT: 100,000 SEK
- Outgoing VAT: 25,000 SEK
- Incoming VAT: 5,000 SEK
```

**MCP Tool Call:**
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

### Example 2: Using a Prompt Template

**User prompt:**
```
Use the monthly VAT creation workflow for company 5567891234, period 2024-01
```

**MCP Prompt Call:**
```json
{
  "name": "create_monthly_vat",
  "arguments": {
    "redovisare": "5567891234",
    "period": "2024-01"
  }
}
```

### Example 3: Reading a Resource

**User prompt:**
```
Show me the VAT draft for company 5567891234, period 2024-01
```

**MCP Resource Read:**
```json
{
  "uri": "vat://drafts/5567891234/2024-01"
}
```

## Development

### Project Structure

```
skatteverket-mcp/
├── src/
│   └── SkatteverketMcpServer/
│       ├── Models/              # Data models
│       ├── Services/            # API client
│       ├── Tools/               # MCP tools
│       ├── Resources/           # MCP resources
│       ├── Prompts/             # MCP prompts
│       ├── Transport/           # JSON-RPC transport
│       ├── McpServer.cs         # Main server logic
│       └── Program.cs           # Entry point
├── tests/
│   └── SkatteverketMcpServer.Tests/
└── docs/
```

### Running Tests

```bash
dotnet test
```

### Running with Debug Logging

```bash
export DOTNET_ENVIRONMENT=Development
dotnet run --project src/SkatteverketMcpServer
```

Logs will be written to `logs/skatteverket-mcp-{date}.log`

## API Mapping

See [docs/API_MAPPING.md](docs/API_MAPPING.md) for detailed mapping between Skatteverket REST endpoints and MCP tools.

## Troubleshooting

### Common Issues

**1. Server not appearing in MCP client**
- Verify the path in client configuration is correct
- Check that .NET 8.0 SDK is installed: `dotnet --version`
- Review client logs for connection errors

**2. Authentication failures**
- Verify API credentials are configured correctly
- Check that certificates are in the correct format
- Ensure OAuth tokens haven't expired

**3. Tool execution errors**
- Check server logs in `logs/` directory
- Verify Skatteverket API is accessible
- Confirm API endpoints are correct for your environment

### Debug Mode

Enable verbose logging by setting environment variable:
```bash
export Serilog__MinimumLevel__Default=Debug
```

## CI/CD

This project uses GitHub Actions for continuous integration and deployment:

- **CI/CD Pipeline**: Builds, tests, and creates artifacts on every push/PR
- **PR Checks**: Validates code quality, formatting, and dependencies
- **Release Automation**: Creates releases with platform-specific binaries
- **Dependabot**: Automatically updates dependencies weekly

See [CI/CD Documentation](docs/CI_CD.md) for detailed information.

### Build Status

| Workflow | Status |
|----------|--------|
| CI/CD Pipeline | [![CI/CD](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml) |
| PR Checks | [![PR Checks](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/pr-checks.yml/badge.svg)](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/pr-checks.yml) |

## Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the code style
4. Run tests locally: `dotnet test`
5. Format code: `dotnet format`
6. Commit using semantic commit messages (`feat:`, `fix:`, `docs:`, etc.)
7. Push to your fork and submit a pull request

**Before submitting:**
- ✅ All tests pass
- ✅ Code is formatted (`dotnet format`)
- ✅ No build warnings
- ✅ Documentation is updated

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines (if available).

## License

MIT License - see LICENSE file for details

## Related Links

- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [Skatteverket API Documentation](https://github.com/virginprogrammer/skatteverket)
- [MCP C# SDK](https://github.com/microsoft/mcp-dotnet)

## Support

For issues and questions:
- GitHub Issues: https://github.com/virginprogrammer/skatteverket-mcp/issues
- MCP Community: https://discord.gg/modelcontextprotocol

## Acknowledgments

- Built with the Model Context Protocol by Anthropic
- Integrates with Skatteverket API
- Uses StreamJsonRpc for JSON-RPC 2.0 implementation
