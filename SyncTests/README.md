# LinkSafe-Kisi Synchronization Test Suite

This test suite provides comprehensive testing for the LinkSafe-Kisi synchronization service, covering unit tests, integration tests, and end-to-end scenarios.

## Test Structure

### Unit Tests

#### StaticHelpersTests.cs
Tests for the `StaticHelpers` utility class:
- Date comparison with timezone tolerance
- Rate limiting for batch operations
- Error handling in batch operations

#### MatchedModelTests.cs
Tests for the `MatchedModel` class that handles worker-contractor matching:
- Constructor validation and error handling
- Compliance status determination
- Date range calculations for validity periods
- Group link model creation
- Edge cases for expired/future inductions

#### ErrorServiceTests.cs
Tests for the `ErrorService` class that handles error logging:
- Error message collection and storage
- Email configuration handling
- Disposal behavior
- Error clearing after sending

#### LinkSafeTests.cs
Tests for the `LinkSafe` service that interacts with the LinkSafe API:
- Service initialization
- API response handling
- JSON deserialization
- Worker-contractor matching logic

#### KisisTests.cs
Tests for the `Kisis` service that manages group links:
- Group link creation and removal
- Name formatting for group links
- API interaction patterns
- Configuration handling

#### TimerTests.cs
Tests for the main `Timer` synchronization logic:
- Complete synchronization workflows
- Worker addition, update, and removal scenarios
- Error handling and logging
- Mixed compliance scenarios

### Integration Tests

#### IntegrationTests.cs
End-to-end tests that verify the complete synchronization flow:
- Real data structure validation
- JSON serialization/deserialization
- Configuration integration
- Error service integration
- Complete workflow scenarios

## Test Categories

1. **Unit Tests** - Individual component testing with mocked dependencies
2. **Integration Tests** - Component interaction testing with real data structures
3. **JSON Serialization Tests** - API data format validation
4. **Configuration Tests** - Service configuration validation
5. **Error Handling Tests** - Exception and error scenario testing

## Test Data

The tests use realistic test data that mirrors the actual API responses:
- Worker models with induction records
- Contractor models with compliance records
- Group link models with validity periods
- Various compliance scenarios (compliant, non-compliant, expired, future)

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=StaticHelpersTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed output
dotnet test --verbosity normal
```

## Test Coverage

The test suite aims to achieve comprehensive coverage of:
- All public methods and properties
- Error conditions and edge cases
- Configuration scenarios
- Data validation and transformation
- API interaction patterns
- Synchronization logic flows

## Dependencies

- **xUnit** - Testing framework
- **FluentAssertions** - Fluent assertion library
- **Moq** - Mocking framework
- **Microsoft.Extensions.Options** - Configuration testing
- **Microsoft.Extensions.Logging.Abstractions** - Logging testing

## Notes

- Tests are designed to be independent and can run in any order
- Mock objects are used to isolate components from external dependencies
- Real data structures are used in integration tests to validate end-to-end flows
- Error scenarios are thoroughly tested to ensure robust error handling

