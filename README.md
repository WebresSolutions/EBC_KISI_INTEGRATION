# LinkSafe-Kisi Synchronization Service

An Azure Functions application that automatically synchronizes worker access permissions between LinkSafe (compliance management) and Kisi (access control) systems. The service ensures that only compliant workers with valid inductions have access to controlled areas.

## Overview

This service bridges two critical systems:
- **LinkSafe**: Manages worker compliance and induction records
- **Kisi**: Controls physical access through group-based permissions

The synchronization process runs automatically on a daily schedule and can also be triggered manually via HTTP endpoints.

## Features

- **Automated Synchronization**: Daily timer-triggered synchronization at midnight
- **Manual Execution**: HTTP endpoint for on-demand synchronization
- **Compliance Validation**: Only workers with valid, non-expired inductions get access
- **Real-time Updates**: Adds, updates, and removes access permissions based on current compliance status
- **Error Handling**: Comprehensive error logging and email notifications
- **Fallback Data**: Uses local JSON files for testing and development
- **Comprehensive Testing**: Full test suite with unit, integration, and end-to-end tests

## Architecture

### Core Components

- **Timer.cs**: Main Azure Function with timer and HTTP triggers
- **LinkSafe.cs**: Service for interacting with LinkSafe API
- **Kisis.cs**: Service for managing Kisi group links and access permissions
- **ErrorService.cs**: Centralized error handling and email notifications
- **MatchedModel.cs**: Data model for matching workers with contractors and compliance status

### Data Flow

1. **Data Collection**: Retrieves workers and contractors from LinkSafe API
2. **Compliance Check**: Validates worker induction status and validity periods
3. **Synchronization**: Updates Kisi group links based on current compliance status
4. **Cleanup**: Removes access for non-compliant or expired workers
5. **Error Reporting**: Sends email notifications for any errors encountered

## API Integration

### LinkSafe API
- **Endpoint**: `GET /2.0/Compliance/Workers/List`
- **Purpose**: Retrieves all workers with their induction records
- **Authentication**: API key via `apikey` header

### Kisi API
- **Endpoints**:
  - `GET /group_links` - Retrieves existing group links (paginated)
  - `POST /group_links` - Creates new group links
  - `DELETE /group_links/{id}` - Removes group links
- **Authentication**: Bearer token via `Authorization` header

## Configuration

### Required Settings

```json
{
  "LinkSafeConfig": {
    "ApiToken": "your-linksafe-api-token"
  },
  "KisisConfig": {
    "ApiToken": "your-kisi-api-token",
    "GroupId": 123,
    "NamePrefix": "LinkSafe"
  },
  "EmailConfig": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "your-email@example.com",
    "Password": "your-password",
    "FromEmail": "noreply@example.com",
    "ToEmail": "admin@example.com"
  }
}
```

### Environment Variables

- `LinkSafeConfig__ApiToken`: LinkSafe API authentication token
- `KisisConfig__ApiToken`: Kisi API authentication token
- `KisisConfig__GroupId`: Target group ID in Kisi system
- `KisisConfig__NamePrefix`: Prefix for group link names
- `EmailConfig__*`: SMTP configuration for error notifications

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools v4
- Valid API tokens for both LinkSafe and Kisi systems
- SMTP server for error notifications

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd EBC_KISI_INTEGRATION
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   - Copy `local.settings.json.example` to `local.settings.json`
   - Update with your API tokens and configuration

4. **Run locally**
   ```bash
   func start
   ```

### Deployment

1. **Build the project**
   ```bash
   dotnet build --configuration Release
   ```

2. **Publish to Azure**
   ```bash
   func azure functionapp publish <function-app-name>
   ```

3. **Configure application settings** in Azure portal with your API tokens and configuration

## Usage

### Automatic Synchronization

The service runs automatically every day at midnight (UTC) via the timer trigger.

### Manual Synchronization

Trigger synchronization manually via HTTP:

```bash
# GET request
curl https://your-function-app.azurewebsites.net/api/HttpTrigger

# POST request
curl -X POST https://your-function-app.azurewebsites.net/api/HttpTrigger
```

### Response Format

```json
{
  "message": "Successfully completed the synchronization of linksafe and kisi! Removed: 5 workers. Added 12 workers. Updated 3 workers.",
  "added": 12,
  "updated": 3,
  "deleted": 5
}
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=TimerTests"
```

### Test Structure

- **Unit Tests**: Individual component testing with mocked dependencies
- **Integration Tests**: End-to-end workflow testing with real data structures
- **JSON Serialization Tests**: API data format validation
- **Configuration Tests**: Service configuration validation
- **Error Handling Tests**: Exception and error scenario testing

## Monitoring and Logging

### Application Insights

The service is configured with Application Insights for:
- Performance monitoring
- Error tracking
- Custom metrics
- Live metrics filtering

### Error Handling

- All errors are logged to the ErrorService
- Email notifications sent for any errors encountered
- Comprehensive error context and stack traces
- Automatic error clearing after notification

## Development

### Project Structure

```
Itm.LinkSafeKisiSynchronisation/
├── Program.cs                 # Application entry point
├── Timer.cs                  # Main Azure Function
├── LinkSafe.cs               # LinkSafe API service
├── Kisis.cs                  # Kisi API service
├── ErrorService.cs           # Error handling service
├── StaticHelpers.cs          # Utility functions
├── Models/                   # Data models
├── LinkSafeModels/           # LinkSafe-specific models
├── KisisModels/              # Kisi-specific models
└── Json/                     # Test data files

SyncTests/                    # Test project
├── UnitTest1.cs
├── IntegrationTests.cs
├── TimerTests.cs
├── LinkSafeTests.cs
├── KisisTests.cs
├── MatchedModelTests.cs
└── StaticHelpersTests.cs
```

### Key Models

- **WorkerModel**: Represents a worker with induction records
- **ContractorModel**: Represents a contractor with compliance data
- **MatchedModel**: Links workers with contractors and determines compliance
- **GroupLinksModel**: Represents Kisi group link data
- **GroupLinkCreateModel**: Model for creating new group links

## Troubleshooting

### Common Issues

1. **API Authentication Errors**
   - Verify API tokens are correct and not expired
   - Check API endpoint URLs and headers

2. **Synchronization Failures**
   - Check error logs in Application Insights
   - Verify network connectivity to both APIs
   - Ensure proper configuration values

3. **Email Notifications Not Working**
   - Verify SMTP configuration
   - Check firewall settings for SMTP port
   - Validate email credentials

### Debug Mode

In debug mode, the service uses local JSON files (`Json/workers.json`, `Json/contractors.json`) as fallback data when API calls fail or return empty results.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

[Add your license information here]

## Support

For support and questions:
- Create an issue in the repository
- Contact the development team
- Check the troubleshooting section above
