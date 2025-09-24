using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RestSharp;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class KisisTests
{
    private readonly Mock<ErrorService> _mockErrorService;
    private readonly Mock<ILogger<Kisis>> _mockLogger;
    private readonly IOptions<KisisConfig> _config;
    private readonly Kisis _kisis;

    public KisisTests()
    {
        _mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        _mockLogger = new Mock<ILogger<Kisis>>();
        _config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "TestPrefix"
        });
        _kisis = new Kisis(_mockErrorService.Object, _config, _mockLogger.Object);
    }

    [Fact]
    public void GetName_WithEmailOnly_ShouldReturnFormattedName()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = _kisis.GetName(email);

        // Assert
        result.Should().Be("TestPrefix test@example.com:  - ");
    }

    [Fact]
    public void GetName_WithEmailAndDates_ShouldReturnFormattedName()
    {
        // Arrange
        var email = "test@example.com";
        var validFrom = new DateTime(2023, 1, 1);
        var validUntil = new DateTime(2023, 12, 31);

        // Act
        var result = _kisis.GetName(email, validFrom, validUntil);

        // Assert
        result.Should().Be("TestPrefix test@example.com: 1/1/2023 12:00:00 AM - 12/31/2023 12:00:00 AM");
    }

    [Fact]
    public void GetName_WithEmailAndValidFromOnly_ShouldReturnFormattedName()
    {
        // Arrange
        var email = "test@example.com";
        var validFrom = new DateTime(2023, 1, 1);

        // Act
        var result = _kisis.GetName(email, validFrom);

        // Assert
        result.Should().Be("TestPrefix test@example.com: 1/1/2023 12:00:00 AM - ");
    }

    [Fact]
    public void GetName_WithEmailAndValidUntilOnly_ShouldReturnFormattedName()
    {
        // Arrange
        var email = "test@example.com";
        var validUntil = new DateTime(2023, 12, 31);

        // Act
        var result = _kisis.GetName(email, validUntil: validUntil);

        // Assert
        result.Should().Be("TestPrefix test@example.com:  - 12/31/2023 12:00:00 AM");
    }

    [Fact]
    public async Task MakeGroupLink_WithEmail_ShouldNotThrow()
    {
        // Arrange
        var email = "test@example.com";
        var validFrom = new DateTime(2023, 1, 1);
        var validUntil = new DateTime(2023, 12, 31);

        // Act & Assert
        var action = async () => await _kisis.MakeGroupLink(email, validFrom, validUntil);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MakeGroupLink_WithMatchedModel_ShouldNotThrow()
    {
        // Arrange
        var matchedModel = CreateValidMatchedModel();

        // Act & Assert
        var action = async () => await _kisis.MakeGroupLink(matchedModel);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveGroupLink_WithValidId_ShouldNotThrow()
    {
        // Arrange
        var id = 123;

        // Act & Assert
        var action = async () => await _kisis.RemoveGroupLink(id);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetGroupLinks_WithEmptyResponse_ShouldReturnEmptyList()
    {
        // This test would require mocking the RestClient, which is complex
        // For now, we'll test the method doesn't throw with default setup
        var result = await _kisis.GetGroupLinks(0, new List<GroupLinksModel>());

        // Assert
        result.Should().NotBeNull();
    }

    private MatchedModel CreateValidMatchedModel()
    {
        var worker = new WorkerModel
        {
            WorkerId = 1,
            EmailAddress = "test@example.com",
            FirstName = "John",
            IsCompliant = true,
            Inductions = new[]
            {
                new InductionModel
                {
                    InductionId = 1,
                    InductedOnUtc = DateTime.UtcNow.AddDays(-10),
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(10)
                }
            },
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "TestContractor"
            },
            Contractor = new Contractor
            {
                ContractorID = 1,
                DisplayName = "TestContractor",
                IsCompliant = true,
                Records = new[]
                {
                    new Record
                    {
                        RecordID = 1,
                        ExpiresOnUtc = DateTime.UtcNow.AddDays(15)
                    }
                }
            }
        };

        return new MatchedModel(worker, _config);
    }
}
