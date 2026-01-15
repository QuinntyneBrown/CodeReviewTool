# Repository Service

A microservice for managing Git repository connections and operations.

## Purpose

The Repository Service manages connections to Git repositories and handles repository operations including:

- Repository registration and configuration
- Git provider integration (GitHub, GitLab, Bitbucket, Azure DevOps)
- Repository metadata caching
- Branch and commit tracking

## Technologies

- **.NET 9.0** - Framework
- **Git CLI** - Git operations
- **UDP Messaging** - Inter-service communication
- **MessagePack** - Message serialization
- **In-Memory Storage** - Data persistence (can be replaced with Couchbase.Lite or other data stores)

## Architecture

The service follows Clean Architecture principles with three main layers:

### Core (RepositoryService.Core)
- Domain entities (Repository, Branch, Commit, PullRequest)
- Message definitions for pub/sub
- Service interfaces

### Infrastructure (RepositoryService.Infrastructure)
- Repository implementations (in-memory)
- Git command-line service
- Git provider adapters
- UDP messaging publisher
- Background services for monitoring

### API (RepositoryService.Api)
- REST API controllers
- Dependency injection configuration
- Swagger documentation

## Messages Published

| Message | Description |
|---------|-------------|
| `repository.registered` | New repository added |
| `repository.updated` | Repository configuration changed |
| `repository.deleted` | Repository removed |
| `repository.push.detected` | New commits pushed |
| `repository.pr.created` | Pull request created |
| `repository.pr.updated` | Pull request updated |
| `repository.pr.merged` | Pull request merged |

## Configuration

Configure the service via `appsettings.json`:

```json
{
  "Messaging": {
    "Host": "127.0.0.1",
    "Port": 9000
  }
}
```

## Running the Service

```bash
# Navigate to the API project
cd src/RepositoryService/src/RepositoryService.Api

# Run the service
dotnet run
```

The service will be available at `http://localhost:5003` (HTTP) or `https://localhost:7003` (HTTPS).

## API Endpoints

### Repositories

- `GET /api/repositories` - List all repositories
- `GET /api/repositories/{id}` - Get a specific repository
- `POST /api/repositories` - Register a new repository
- `PUT /api/repositories/{id}` - Update a repository
- `DELETE /api/repositories/{id}` - Delete a repository

### Example: Register a Repository

```bash
curl -X POST http://localhost:5003/api/repositories \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MyRepo",
    "url": "https://github.com/user/repo.git",
    "localPath": "/repos/myrepo",
    "provider": "GitHub",
    "defaultBranch": "main"
  }'
```

## Background Services

The Repository Monitor Service runs every 5 minutes to:

1. Pull latest changes from registered repositories
2. Detect new commits on branches
3. Update branch metadata
4. Publish push.detected messages for new commits

## Development

### Building

```bash
dotnet build RepositoryService.sln
```

### Testing

```bash
dotnet test
```

## Integration with API Gateway

Add the following to the API Gateway's `appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "repository-service-route": {
        "ClusterId": "repository-service-cluster",
        "Match": {
          "Path": "/api/repositories/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "repository-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5003"
          }
        }
      }
    }
  }
}
```

## Future Enhancements

- Complete Couchbase.Lite integration for persistent storage
- Full Git provider adapter implementations (GitHub, GitLab, Bitbucket, Azure DevOps)
- Webhook support for real-time repository events
- Authentication and authorization
- Rate limiting for Git operations
- Repository health monitoring
