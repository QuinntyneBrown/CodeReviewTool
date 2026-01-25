# Code Review Tool

A comprehensive code review and analysis platform built with .NET 9 and Angular.

## Architecture

The application follows a microservices architecture with an API Gateway pattern and includes a CLI tool for command-line usage:

### Services

1. **API Gateway** (`src/ApiGateway`)
   - Entry point for all frontend requests
   - Routes requests to appropriate backend services
   - Provides CORS configuration for frontend communication
   - Built with [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/)

2. **Git Analysis Service** (`src/GitAnalysis`)
   - Analyzes Git repository differences
   - Compares branches and generates detailed diff reports
   - Endpoints: `/api/comparison/*`

3. **Realtime Notification Service** (`src/RealtimeNotification`)
   - Provides real-time notifications via SignalR
   - WebSocket support for live updates
   - Hub endpoint: `/notifications`

4. **Repository Service** (`src/RepositoryService`)
   - Manages Git repository connections and operations
   - Tracks repository metadata, branches, and commits
   - Publishes events for repository changes
   - Endpoints: `/api/repositories/*`
   - Runs on `http://localhost:5003` (development)

5. **Frontend** (`src/Ui`)
   - Angular-based user interface
   - Communicates exclusively through the API Gateway
   - Runs on `http://localhost:4200` (development)

6. **CLI Tool** (`src/CodeReviewTool.Cli`)
   - Command-line interface for branch comparison
   - Standalone tool that can be used without the web interface
   - Generates formatted console output

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) (for frontend)
- Git

### Building the Solution

```bash
# Clone the repository
git clone https://github.com/QuinntyneBrown/CodeReviewTool.git
cd CodeReviewTool

# Build all projects
dotnet build CodeReviewTool.sln
```

### Using the CLI Tool

The CLI tool (`crt`) allows you to compare Git branches directly from the command line:

```bash
# Build the CLI
dotnet build src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj

# Compare branches (defaults: from=current branch, into=main)
dotnet run --project src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj

# Specify branches explicitly
dotnet run --project src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj -- -f feature/my-branch -i develop

# Specify a different repository
dotnet run --project src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj -- -f main -i feature/test -r /path/to/repo

# Enable verbose logging for debugging
dotnet run --project src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj -- -f feature/my-branch -i develop --verbose

# View help
dotnet run --project src/CodeReviewTool.Cli/CodeReviewTool.Cli.csproj -- --help
```

#### CLI Options

- `-f, --from <branch>` - The branch to compare from (default: current branch)
- `-i, --into <branch>` - The branch to compare into (default: main)
- `-r, --repository <path>` - Path to the Git repository (default: current directory)
- `-v, --verbose` - Enable verbose logging output for debugging

The CLI will display:
- Number of files changed
- Total additions and deletions
- List of changed files with their individual stats
- Color-coded output for better readability

### Running the Services

#### 1. Start the Backend Services

```bash
# Terminal 1: Git Analysis Service (Port 5001)
cd src/GitAnalysis/src/GitAnalysis.Api
dotnet run

# Terminal 2: Realtime Notification Service (Port 5002)
cd src/RealtimeNotification/src/RealtimeNotification.Api
dotnet run

# Terminal 3: Repository Service (Port 5003)
cd src/RepositoryService/src/RepositoryService.Api
dotnet run

# Terminal 4: API Gateway (Port 5000)
cd src/ApiGateway
dotnet run
```

#### 2. Start the Frontend

```bash
# Terminal 5: Angular Frontend
cd src/Ui
npm install
npm start
```

The application will be available at `http://localhost:4200`.

## API Gateway Configuration

The API Gateway is configured via `appsettings.json` and uses YARP for reverse proxying:

### Routes

- **Git Analysis**: `/api/comparison/*` → `http://localhost:5001`
- **Notifications**: `/notifications/*` → `http://localhost:5002`
- **Repository Service**: `/api/repositories/*` → `http://localhost:5003`

### CORS

Configured to allow requests from `http://localhost:4200` (frontend) with:
- Any HTTP method
- Any headers
- Credentials support

### Configuration Example

```json
{
  "ReverseProxy": {
    "Routes": {
      "git-analysis-route": {
        "ClusterId": "git-analysis-cluster",
        "Match": {
          "Path": "/api/comparison/{**catch-all}"
        }
      },
      "realtime-notification-route": {
        "ClusterId": "realtime-notification-cluster",
        "Match": {
          "Path": "/notifications/{**catch-all}"
        }
      },
      "repository-service-route": {
        "ClusterId": "repository-service-cluster",
        "Match": {
          "Path": "/api/repositories/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "git-analysis-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001"
          }
        }
      },
      "realtime-notification-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5002"
          }
        }
      },
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

## Testing

### Running All Tests

```bash
dotnet test CodeReviewTool.sln
```

### Running Specific Test Projects

```bash
# API Gateway Tests
dotnet test tests/ApiGateway.Tests/ApiGateway.Tests.csproj

# Git Analysis Tests
dotnet test src/GitAnalysis/GitAnalysis.sln

# Realtime Notification Tests
dotnet test src/RealtimeNotification/RealtimeNotification.sln

# Repository Service Tests
dotnet test src/RepositoryService/RepositoryService.sln

# CLI Tests
dotnet test tests/CodeReviewTool.Cli.Tests/CodeReviewTool.Cli.Tests.csproj
```

### Test Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

The API Gateway test suite achieves >80% code coverage with comprehensive tests for:
- Routing configuration
- CORS policies
- Reverse proxy functionality
- Backend service integration

## Project Structure

```
CodeReviewTool/
├── src/
│   ├── ApiGateway/                    # API Gateway service
│   ├── CodeReviewTool.Shared/         # Shared messaging infrastructure
│   ├── GitAnalysis/                   # Git analysis microservice
│   ├── RealtimeNotification/          # Notification microservice
│   ├── RepositoryService/             # Repository management microservice
│   ├── Ui/                            # Angular frontend
│   └── CodeReviewTool.Cli/            # CLI tool
├── tests/
│   ├── ApiGateway.Tests/              # API Gateway tests
│   ├── GitAnalysis/                   # Git Analysis tests
│   ├── RealtimeNotification/          # Notification tests
│   ├── CodeReviewTool.Tests/          # Integration tests
│   └── CodeReviewTool.Cli.Tests/      # CLI tests
└── CodeReviewTool.sln                 # Main solution file
```

## Shared Infrastructure

The `CodeReviewTool.Shared` project provides common infrastructure for inter-service communication:

### Messaging

- **UDP-based messaging** for low-latency service-to-service communication
- **MessagePack serialization** for efficient binary message encoding
- **Pub/Sub pattern** for loose coupling between services

### Message Flow

1. Services publish messages to a UDP multicast address
2. Interested services subscribe to specific message types
3. Messages are automatically serialized/deserialized
4. No central broker required for message delivery

See the [Repository Service README](src/RepositoryService/README.md) for examples of message publishing.

## Development

### API Gateway

The API Gateway uses YARP for high-performance reverse proxying. Key features:

- **Request Forwarding**: Transparent proxying to backend services
- **Path Matching**: Route-based request routing
- **Load Balancing**: Support for multiple destination instances
- **Health Checks**: Monitor backend service availability

### Adding New Routes

To add a new route to the API Gateway:

1. Add route configuration in `appsettings.json`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "new-service-route": {
        "ClusterId": "new-service-cluster",
        "Match": {
          "Path": "/api/newservice/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "new-service-cluster": {
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

2. Add tests in `tests/ApiGateway.Tests/`

3. Update this README with the new route information

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE.txt file for details.

## Contact

Quinntyne Brown - [@QuinntyneBrown](https://github.com/QuinntyneBrown)

Project Link: [https://github.com/QuinntyneBrown/CodeReviewTool](https://github.com/QuinntyneBrown/CodeReviewTool)
