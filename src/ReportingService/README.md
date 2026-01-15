# Reporting Service

The Reporting Service is responsible for generating various types of reports in multiple formats.

## Features

- **Multiple Report Formats**:
  - HTML reports
  - PDF reports
  - SARIF reports
  - JUnit XML reports
  - CSV exports

- **Report Types**:
  - Analysis reports (code review analysis)
  - Analytics reports (metrics and statistics)
  - Compliance reports (policy evaluation)

- **Message-Based Architecture**:
  - Publishes: `report.generated`, `report.generation.failed`
  - Consumes: `analysis.completed`, `analytics.report.requested`, `policy.evaluated`

## Architecture

The service follows a clean architecture pattern with three layers:

- **Core**: Domain entities, interfaces, and messages
- **Infrastructure**: Implementations of services, repositories, and message handlers
- **Api**: RESTful API endpoints for report management

## Storage

Reports are stored in:
- **Metadata**: CouchbaseLite database
- **Content**: Local file system (configurable for cloud storage like S3/Azure Blob)

## API Endpoints

- `GET /api/reports` - Get all reports
- `GET /api/reports/{id}` - Get a specific report
- `GET /api/reports/type/{type}` - Get reports by type
- `POST /api/reports/generate/{format}` - Generate a new report
- `DELETE /api/reports/{id}` - Delete a report

## Configuration

Configure messaging settings in `appsettings.json`:

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
cd src/ReportingService/src/ReportingService.Api
dotnet run
```

The service will be available at `https://localhost:5001` (or the configured port).
