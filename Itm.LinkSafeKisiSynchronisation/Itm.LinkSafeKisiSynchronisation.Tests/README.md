# Unit Tests for LinkSafe Kisi Synchronisation

This project contains comprehensive unit tests for the LinkSafe Kisi Synchronisation Azure Function.

## Test Coverage

The test suite covers the following components:

### 1. StaticHelpers Tests
- `AreDatesEqualIgnoringTimeZone` - Tests date comparison with timezone tolerance
- `RunWithRateLimit` - Tests batch processing with rate limiting

### 2. MatchedModel Tests
- Constructor validation with various worker states
- Compliance checking logic
- Group link model creation
- Edge cases for expired inductions and non-compliant workers

### 3. ErrorService Tests
- Error logging functionality
- Timestamp handling
- Multiple error accumulation

### 4. Kisis Tests
- Group link name generation
- Group link creation and removal operations
- API interaction methods

### 5. LinkSafe Tests
- Worker and contractor data retrieval
- Worker-contractor matching logic

### 6. Timer Tests
- HTTP trigger functionality
- Timer trigger execution
- Synchronization process logic
- Error handling and logging

### 7. Timer Private Method Tests
- `GrooupLinkNeedsToBeUpdated` method testing using reflection
- Various comparison scenarios for group link updates

## Running the Tests

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Command Line
```bash
# Navigate to the test project directory
cd Itm.LinkSafeKisiSynchronisation.Tests

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio
1. Open the solution in Visual Studio
2. Go to Test → Test Explorer
3. Click "Run All Tests" or run individual test classes

## Test Dependencies

The tests use the following NuGet packages:
- **xunit** - Testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Fluent assertion library
- **Microsoft.Extensions.Options** - Configuration options
- **Microsoft.Extensions.Logging** - Logging abstractions

## Mocking Strategy

The tests use Moq to mock external dependencies:
- `ILogger` and `ILoggerFactory` for logging
- `ErrorService` for error handling
- `LinkSafe` and `Kisis` services for API interactions
- `IOptions<T>` for configuration

## Test Data

Test data is created using factory methods within each test class to ensure:
- Consistent test data across tests
- Easy maintenance and updates
- Realistic data scenarios

## Coverage Goals

The test suite aims for:
- 100% method coverage for public methods
- 90%+ line coverage for critical business logic
- Comprehensive edge case testing
- Error scenario validation

## Notes

- Some tests use reflection to test private methods (e.g., `GrooupLinkNeedsToBeUpdated`)
- Integration tests would require additional setup for external API mocking
- Tests are designed to run in isolation without external dependencies
