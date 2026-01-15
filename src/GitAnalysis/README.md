# GitAnalysis

Microservice for analyzing Git repository changes and generating diffs between branches.

## Features

- Generate diffs between Git branches
- Compare current branch against main/master branch
- Support for both native git CLI and LibGit2Sharp implementations
- .gitignore pattern support
- Asynchronous comparison processing
- RESTful API for Git operations

## Configuration

The GitAnalysis service can be configured to use either the native git command-line tool (default) or LibGit2Sharp library.

### appsettings.json

```json
{
  "GitService": {
    "UseNativeGit": true,
    "DefaultBaseBranch": "main"
  }
}
```

### Configuration Options

- **UseNativeGit** (default: `true`): When `true`, uses native git CLI without external libraries. When `false`, uses LibGit2Sharp.
- **DefaultBaseBranch** (default: `"main"`): The default branch to compare against when not specified.

## Native Git CLI Implementation

The default implementation uses the native git command-line tool, which:
- Requires no external libraries (LibGit2Sharp is optional)
- Works with any Git repository accessible via the git command
- Automatically detects and uses main/master branch when base branch is not specified
- Provides full diff information including line-level changes

## API Endpoints

### POST /api/comparison
Request a new Git comparison between branches.

### GET /api/comparison/{requestId}
Get the status and result of a comparison request.

### GET /api/comparison/branches
Get all branches in a repository.
