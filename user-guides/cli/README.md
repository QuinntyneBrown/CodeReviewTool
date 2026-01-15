# CodeReviewTool CLI User Guide

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Command-Line Options](#command-line-options)
- [Features](#features)
  - [Branch Comparison](#branch-comparison)
  - [Line-by-Line Diff](#line-by-line-diff)
  - [Static Analysis](#static-analysis)
- [Usage Examples](#usage-examples)
- [Output Interpretation](#output-interpretation)
- [Advanced Usage](#advanced-usage)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## Overview

**CodeReviewTool CLI** (`crt`) is a powerful command-line tool for comparing Git branches, analyzing code changes, and performing static analysis on modified files. It provides comprehensive insights into code differences and quality issues, making code reviews more efficient and thorough.

### Key Features

- 🔍 **Branch Comparison** - Compare any two Git branches to see what's changed
- 📊 **Detailed Statistics** - View additions, deletions, and modifications counts
- 📝 **Line-by-Line Diffs** - See exact changes with color-coded output
- 🔬 **Static Analysis** - Automatically analyze changed files for code quality issues
- 🎨 **Color-Coded Output** - Easy-to-read terminal output with syntax highlighting
- ⚡ **Fast & Efficient** - Lightweight tool with minimal dependencies

---

## Installation

### Prerequisites

- **.NET 9.0 SDK** or later
- **Git** installed and configured
- A valid Git repository

### Building from Source

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd CodeReviewTool
   ```

2. Build the project:
   ```bash
   cd src/CodeReviewTool.Cli
   dotnet build
   ```

3. Run the tool:
   ```bash
   dotnet run -- [options]
   ```

### Creating a Global Tool

To use `crt` from anywhere:

1. Pack the tool:
   ```bash
   dotnet pack
   ```

2. Install globally:
   ```bash
   dotnet tool install --global --add-source ./nupkg CodeReviewTool.Cli
   ```

3. Use anywhere:
   ```bash
   crt [options]
   ```

---

## Quick Start

### Basic Usage

Compare current branch with `main`:
```bash
dotnet run -- -r /path/to/repository
```

Compare specific branches:
```bash
dotnet run -- -r /path/to/repository -f feature-branch -i main
```

Show detailed line-by-line diffs:
```bash
dotnet run -- -r /path/to/repository -d
```

Run without static analysis:
```bash
dotnet run -- -r /path/to/repository --analyze false
```

---

## Command-Line Options

### Required Options

None - all options have sensible defaults.

### Optional Parameters

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--from` | `-f` | Branch to compare from | Current branch |
| `--into` | `-i` | Branch to compare into | `main` |
| `--repository` | `-r` | Path to Git repository | Current directory |
| `--verbose` | `-v` | Enable verbose logging | `false` |
| `--show-diff` | `-d` | Show line-by-line diffs | `false` |
| `--analyze` | `-a` | Run static analysis | `true` |
| `--help` | `-h`, `-?` | Show help information | - |
| `--version` | - | Show version information | - |

### Examples

```bash
# Compare feature branch with main
dotnet run -- -f feature-branch -i main

# Analyze specific repository with verbose output
dotnet run -- -r C:\projects\MyRepo -v

# Full analysis with diffs
dotnet run -- -r C:\projects\MyRepo -d -a

# Compare current branch with develop, show diffs
dotnet run -- -i develop -d

# Disable analysis, just show changes
dotnet run -- --analyze false
```

---

## Features

### Branch Comparison

The tool compares two Git branches and identifies all changed files, providing:

- **File Change Type**: Added (+), Modified (M), or Deleted (-)
- **Line Statistics**: Number of additions and deletions per file
- **Aggregate Metrics**: Total changes across all files

**Example Output:**
```
Files changed:     42
Total additions:   +347
Total deletions:   -89
Total modified:    15

Changed Files:
─────────────────────────────────────────────────────────────
  M src/Services/UserService.cs                   +  23 -   5
  + src/Models/UserProfile.cs                     +  45 -   0
  M tests/UserServiceTests.cs                     +  67 -  12
  - src/Legacy/OldService.cs                      +   0 -  89
```

### Line-by-Line Diff

Enable with `--show-diff` or `-d` flag to see detailed code changes.

**Features:**
- Color-coded lines (green for additions, red for deletions, gray for context)
- Line numbers for easy reference
- Organized by file for clarity
- Shows only changed sections with context

**Example Output:**
```
──── src/Services/UserService.cs ────
  +  15 | using System.Security.Claims;
  +  16 | using Microsoft.Extensions.Logging;
     17 | 
  -  18 | public class UserService
  +  18 | public class UserService : IUserService
     19 | {
  +  20 |     private readonly ILogger<UserService> _logger;
  +  21 |
```

**Color Legend:**
- 🟢 **Green (+)**: Added lines
- 🔴 **Red (-)**: Deleted lines
- ⚪ **Gray**: Context lines (unchanged)

### Static Analysis

Automatically analyzes changed files for code quality issues when enabled (default).

**Supported File Types:**
- `.cs` (C#)
- `.ts` (TypeScript)
- `.js` (JavaScript)

**Analysis Categories:**
- **Violations** (Errors): Critical issues that should be fixed
- **Warnings**: Potential problems that should be reviewed
- **Info**: Informational messages and suggestions

**Example Output:**
```
═══════════════════════════════════════════════════════════════
  Static Analysis Results
═══════════════════════════════════════════════════════════════

Analyzing 3 file(s)...

Found 2 violation(s):

  ✗ [AC5.1] Missing or incorrect copyright header.
    Location: UserService.cs:1
    Source: implementation.spec.md
    Fix: Add the following header at the top of the file:
// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

  ✗ [MSG-001] Message class must be marked with [MessagePackObject] attribute.
    Location: UserCreatedMessage.cs:10
    Source: message-design.spec.md

Found 1 warning(s):

  ⚠ [MSG-WARN-01] Consider using record types for messages.
    Location: UserUpdatedMessage.cs:5
    Source: message-design.spec.md
```

**Disabling Analysis:**
```bash
dotnet run -- --analyze false
# or
dotnet run -- -a false
```

---

## Usage Examples

### Example 1: Review Feature Branch

You're ready to merge a feature branch and want a full code review:

```bash
cd C:\projects\MyProject
dotnet run -- -f feature/new-authentication -i main -d -v
```

This will:
1. Compare `feature/new-authentication` with `main`
2. Show detailed line-by-line diffs
3. Run static analysis on changed files
4. Display verbose logging for troubleshooting

### Example 2: Quick Change Summary

You want a quick overview without details:

```bash
dotnet run -- -r C:\projects\MyProject --analyze false
```

This shows only file changes and statistics, skipping diffs and analysis.

### Example 3: Analyze External Repository

Check a repository that's not in your current directory:

```bash
dotnet run -- -r "C:\projects\ExternalRepo" -f develop -i release/1.0
```

### Example 4: Pre-Commit Review

Before committing changes, review what you've modified:

```bash
# Creates a temporary branch from current state
git checkout -b temp-review
dotnet run -- -f temp-review -i main -d
```

### Example 5: CI/CD Integration

Use in a CI pipeline to enforce code quality:

```bash
#!/bin/bash
# Run code review analysis
dotnet run --project src/CodeReviewTool.Cli -- \
  -f "$CI_COMMIT_BRANCH" \
  -i "$CI_DEFAULT_BRANCH" \
  -v

# Check exit code
if [ $? -ne 0 ]; then
  echo "Code review failed!"
  exit 1
fi
```

---

## Output Interpretation

### Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success - analysis completed |
| `1` | Error - invalid repository, missing branch, or other failure |

### Understanding Results

#### File Change Indicators

- `+` (Addition): File was newly created
- `M` (Modified): File was changed
- `-` (Deletion): File was removed

#### Line Counts

- **Additions (+)**: New lines added to the file
- **Deletions (-)**: Lines removed from the file
- **Net Change**: Additions - Deletions

#### Static Analysis Severity

1. **Violations (✗)**: Must be fixed before merging
   - Rule violations that break specifications
   - Critical quality issues
   - Should block merge until resolved

2. **Warnings (⚠)**: Should be reviewed
   - Potential issues that may cause problems
   - Best practice violations
   - Consider fixing but not blocking

3. **Info (ℹ)**: Optional improvements
   - Suggestions for improvement
   - Style recommendations
   - Documentation notes

---

## Advanced Usage

### Comparing Non-Adjacent Branches

```bash
# Compare a feature branch with a release branch
dotnet run -- -f feature/advanced-search -i release/2.0 -d
```

### Filtering Large Outputs

For repositories with many changes, redirect output to a file:

```bash
dotnet run -- -d > review-output.txt
```

### Combining with Git Commands

```bash
# Review changes from last release tag
LAST_TAG=$(git describe --tags --abbrev=0)
dotnet run -- -f HEAD -i $LAST_TAG -d
```

### Using in Scripts

**PowerShell Example:**
```powershell
$repo = "C:\projects\MyRepo"
$from = "feature-branch"
$into = "main"

& dotnet run -- -r $repo -f $from -i $into -d

if ($LASTEXITCODE -ne 0) {
    Write-Error "Code review failed"
    exit 1
}

Write-Host "Code review passed!" -ForegroundColor Green
```

**Bash Example:**
```bash
#!/bin/bash
REPO_PATH="/home/user/projects/MyRepo"

# Run analysis
dotnet run -- -r "$REPO_PATH" -f feature-branch -i main -d -v

# Check result
if [ $? -eq 0 ]; then
    echo "✓ Code review completed successfully"
else
    echo "✗ Code review failed"
    exit 1
fi
```

---

## Troubleshooting

### Common Issues

#### "Not a valid Git repository"

**Problem:** The specified path is not a Git repository.

**Solution:**
```bash
# Verify the path is correct
cd /path/to/repository
git status

# Ensure .git directory exists
ls -la .git
```

#### "Branch does not exist"

**Problem:** The specified branch name is incorrect or doesn't exist.

**Solution:**
```bash
# List all branches
git branch -a

# Use exact branch name
dotnet run -- -f origin/feature-branch -i main
```

#### No Differences Found

**Problem:** Tool reports no changes when you expect some.

**Solution:**
- Ensure you're comparing the correct branches
- Check if branches are in sync:
  ```bash
  git fetch --all
  git log --oneline feature-branch..main
  ```

#### Static Analysis Fails

**Problem:** Analysis crashes or reports errors.

**Solution:**
- Run with verbose logging: `dotnet run -- -v`
- Disable analysis temporarily: `dotnet run -- --analyze false`
- Check file types (only .cs, .ts, .js are supported)

#### Permission Denied

**Problem:** Cannot access repository files.

**Solution:**
```bash
# Check file permissions
ls -la /path/to/repository

# Run with appropriate user permissions
sudo dotnet run -- -r /path/to/repository
```

### Debug Mode

Enable verbose logging for detailed diagnostic information:

```bash
dotnet run -- -r /path/to/repo -v
```

This shows:
- Repository validation steps
- Branch detection logic
- File processing details
- Analysis execution logs

---

## Best Practices

### 1. **Regular Code Reviews**

Run the tool regularly during development:
- Before committing changes
- Before creating pull requests
- After merging branches

### 2. **Use with CI/CD**

Integrate into your pipeline:
- Automate code quality checks
- Block merges with violations
- Generate review reports

### 3. **Customize Analysis**

Adjust flags based on needs:
- Use `-d` for detailed reviews
- Skip analysis for large file moves: `--analyze false`
- Enable verbose mode for debugging: `-v`

### 4. **Branch Naming**

Use consistent branch naming:
- `feature/*` for new features
- `bugfix/*` for bug fixes
- `release/*` for release branches

This makes comparisons more intuitive:
```bash
dotnet run -- -f feature/user-auth -i develop
```

### 5. **Document Findings**

Save review results for reference:
```bash
dotnet run -- -d > code-review-$(date +%Y%m%d).txt
```

### 6. **Team Workflows**

Establish team conventions:
- Define which violations are blocking
- Set up pre-commit hooks
- Create review checklists

### 7. **Performance Tips**

For large repositories:
- Use `--analyze false` for quick checks
- Skip diffs on large file sets
- Focus on specific directories

---

## Additional Resources

### Related Documentation

- [Git Documentation](https://git-scm.com/doc)
- [.NET CLI Reference](https://docs.microsoft.com/dotnet/core/tools/)
- Project specifications in `docs/specs/`

### Getting Help

```bash
# Show all available options
dotnet run -- --help

# Show version
dotnet run -- --version
```

### Contributing

Report issues or suggest features at the project repository.

---

## Version History

### v1.1.0 (Current)
- ✨ Added line-by-line diff display (`--show-diff`)
- ✨ Integrated static analysis for changed files
- 🎨 Improved color-coded output
- 📊 Enhanced statistics reporting

### v1.0.0
- 🎉 Initial release
- ✅ Basic branch comparison
- ✅ File change detection
- ✅ Statistics reporting

---

## License

Copyright (c) Quinntyne Brown. All Rights Reserved.
Licensed under the MIT License. See License.txt in the project root for license information.

---

## Support

For issues, questions, or contributions, please refer to the main project documentation or contact the development team.

**Happy Code Reviewing! 🚀**
