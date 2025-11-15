# CI/CD Documentation

This document describes the CI/CD workflows for the Skatteverket MCP Server project.

## Overview

The project uses GitHub Actions for continuous integration and deployment. There are three main workflows:

1. **CI/CD Pipeline** (`ci.yml`) - Main build and test workflow
2. **PR Checks** (`pr-checks.yml`) - Additional validation for pull requests
3. **Release** (`release.yml`) - Automated release creation and distribution

## Workflows

### 1. CI/CD Pipeline (`ci.yml`)

**Triggers:**
- Push to `main` or `master` branch
- Pull requests to `main` or `master` branch

**Jobs:**

#### build-and-test
Builds the solution and runs all tests.

- ✅ Restores NuGet dependencies
- ✅ Builds in Release configuration
- ✅ Runs unit tests with coverage
- ✅ Uploads test results as artifacts
- ✅ Generates code coverage reports
- ✅ Posts coverage summary on PRs
- ✅ Checks for build warnings

**Artifacts:**
- Test results (TRX files)
- Code coverage reports (Cobertura XML)

#### code-quality
Performs code quality checks.

- ✅ Verifies code formatting (`dotnet format`)
- ✅ Scans for vulnerable packages
- ✅ Checks for deprecated dependencies

#### build-artifacts
Creates release artifacts for multiple platforms (only on push to main).

**Platforms:**
- Windows x64 (`win-x64`)
- Linux x64 (`linux-x64`)
- macOS x64 (`osx-x64`)
- macOS ARM64 (`osx-arm64`)

**Output:**
- Single-file, self-contained executables
- Trimmed for smaller size
- Uploaded as workflow artifacts (90-day retention)

#### summary
Creates a build summary visible in the GitHub UI.

### 2. PR Checks (`pr-checks.yml`)

**Triggers:**
- Pull request opened, synchronized, or reopened

**Jobs:**

#### validate-pr
General PR validation.

- ✅ Checks PR title follows semantic commit format
- ✅ Detects merge conflicts
- ✅ Checks for large files (>5MB)

**Semantic PR Title Format:**
```
<type>[(scope)]: <description>

Types: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
```

Examples:
- `feat: add VAT draft validation tool`
- `fix(api): handle null responses from Skatteverket API`
- `docs: update README with configuration examples`

#### lint-csharp
C# code linting and formatting.

- ✅ Verifies code formatting (`dotnet format`)
- ✅ Analyzes code with warning level 4
- ✅ Treats formatting violations as errors

#### dependency-check
Security and dependency validation.

- ✅ Scans for vulnerable packages
- ✅ Checks for deprecated packages
- ✅ Includes transitive dependencies

#### test-build-matrix
Cross-platform build verification.

**Test Matrix:**
- Ubuntu (Linux)
- Windows
- macOS

Ensures the solution builds and tests pass on all supported platforms.

### 3. Release Workflow (`release.yml`)

**Triggers:**
- Push tags matching `v*.*.*` (e.g., `v1.0.0`)
- Manual workflow dispatch with version input

**Jobs:**

#### create-release
Creates a GitHub release.

- ✅ Extracts version from tag or input
- ✅ Generates changelog from commits
- ✅ Creates GitHub release
- ✅ Marks as prerelease if version contains `alpha`, `beta`, or `rc`

#### build-and-upload
Builds and uploads release assets for all platforms.

**Platforms:**
- Windows x64/x86
- Linux x64/ARM64
- macOS x64/ARM64

**Process:**
1. Build with release version
2. Run tests
3. Publish self-contained executable
4. Create archive (ZIP for Windows, TAR.GZ for Unix)
5. Upload to GitHub release

#### publish-nuget
Publishes to NuGet (stable releases only).

- ✅ Creates NuGet package
- ✅ Pushes to NuGet.org
- ✅ Skips if already published

## Setting Up Workflows

### Required Secrets

Add these secrets in GitHub repository settings:

1. **GITHUB_TOKEN** (automatically provided)
   - Used for: Creating releases, uploading assets

2. **NUGET_API_KEY** (optional)
   - Used for: Publishing to NuGet
   - Get from: https://www.nuget.org/account/apikeys

### Repository Settings

**Branch Protection Rules for `main`:**

1. Navigate to: Settings → Branches → Add rule
2. Branch name pattern: `main`
3. Enable:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
     - Required checks:
       - `Build and Test`
       - `Code Quality Checks`
       - `Lint C# Code`
       - `Test on ubuntu-latest`
   - ✅ Require conversation resolution before merging
   - ✅ Require linear history
   - ✅ Include administrators

## Workflow Triggers

### Automatic Triggers

| Event | Workflows |
|-------|-----------|
| Push to `main` | ci.yml (all jobs including artifacts) |
| Pull request | ci.yml (build/test), pr-checks.yml |
| Tag `v*.*.*` | release.yml |

### Manual Triggers

**Release Workflow:**
```bash
# Via GitHub UI: Actions → Release → Run workflow
# Enter version (e.g., v1.0.0)
```

**Force CI Run:**
```bash
# Push empty commit to trigger CI
git commit --allow-empty -m "ci: trigger workflow"
git push
```

## Viewing Results

### CI Results

1. Navigate to: Actions tab in GitHub
2. Select workflow run
3. View job logs and artifacts

### Test Results

Test results are uploaded as artifacts:
- Download from workflow run summary
- View TRX files in Visual Studio or VS Code

### Code Coverage

For pull requests:
- Coverage summary posted as PR comment
- Coverage badge shown in summary
- Threshold: 60% (warning), 80% (good)

## Creating a Release

### Automated Release (Recommended)

1. **Update version** in project file (optional - will use tag):
   ```xml
   <Version>1.0.0</Version>
   ```

2. **Create and push tag:**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **Workflow automatically:**
   - Creates GitHub release
   - Generates changelog
   - Builds all platforms
   - Uploads artifacts
   - Publishes to NuGet (if configured)

### Manual Release

1. Navigate to: Actions → Release → Run workflow
2. Enter version: `v1.0.0`
3. Click "Run workflow"

### Prerelease

Tag with `alpha`, `beta`, or `rc`:
```bash
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

Release will be marked as prerelease and won't publish to NuGet.

## Dependabot

Automated dependency updates are configured via `.github/dependabot.yml`:

- **NuGet packages**: Weekly on Monday at 09:00
- **GitHub Actions**: Weekly on Monday at 09:00

Dependabot will:
- Create PRs for dependency updates
- Assign to `@virginprogrammer`
- Label with `dependencies` and package type
- Limit concurrent PRs (10 for NuGet, 5 for actions)

## Troubleshooting

### Build Failures

**"Restore failed"**
- Check NuGet package versions are available
- Verify network connectivity
- Check for authentication issues with private feeds

**"Test failures"**
- Review test logs in workflow output
- Download test results artifact
- Run tests locally: `dotnet test --verbosity detailed`

**"Code format check failed"**
- Run locally: `dotnet format`
- Commit formatting changes
- Push updated code

### Release Failures

**"Failed to create release"**
- Ensure tag follows `v*.*.*` format
- Check for existing release with same tag
- Verify GITHUB_TOKEN permissions

**"Failed to upload asset"**
- Check build succeeded for all platforms
- Verify archive creation succeeded
- Check network connectivity

**"NuGet publish failed"**
- Verify NUGET_API_KEY is configured
- Check version doesn't already exist
- Ensure version follows semantic versioning

### Workflow Permissions

If workflows fail with permission errors:

1. Go to: Settings → Actions → General
2. Workflow permissions: Select "Read and write permissions"
3. Save changes

## Monitoring

### Workflow Status Badge

Add to README.md:
```markdown
[![CI/CD](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/virginprogrammer/skatteverket-mcp/actions/workflows/ci.yml)
```

### Notifications

Configure in: Settings → Notifications
- Email on workflow failures
- Slack/Discord webhooks (optional)

## Best Practices

1. **Always run CI locally before pushing:**
   ```bash
   dotnet restore
   dotnet build --configuration Release
   dotnet test --configuration Release
   dotnet format --verify-no-changes
   ```

2. **Keep workflows fast:**
   - Use caching for dependencies
   - Run expensive jobs only on main branch
   - Parallelize independent jobs

3. **Meaningful commit messages:**
   - Follow semantic commit format
   - Reference issues in commits
   - Keep commits focused and atomic

4. **Review Dependabot PRs:**
   - Check changelog for breaking changes
   - Review test results
   - Merge regularly to avoid conflicts

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Actions Setup](https://github.com/actions/setup-dotnet)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
