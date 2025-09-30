using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Record = Itm.LinkSafeKisiSynchronisation.LinkSafeModels.Record;

namespace SyncTests;

public class KisisTests
{
    private readonly Mock<ErrorService> _mockErrorService;
    private readonly Mock<IOptions<KisisConfig>> _mockConfig;
    private readonly Mock<ILogger<Kisis>> _mockLogger;
    private readonly Kisis _kisis;

    public KisisTests()
    {
        _mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        _mockConfig = new Mock<IOptions<KisisConfig>>();
        _mockConfig.Setup(x => x.Value).Returns(new KisisConfig
        {
            ApiToken = "test-api-token",
            GroupId = 123,
            NamePrefix = "TEST"
        });
        _mockLogger = new Mock<ILogger<Kisis>>();

        _kisis = new Kisis(_mockErrorService.Object, _mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        _kisis.Should().NotBeNull();
    }

    [Fact]
    public void GetName_WithEmailOnly_ShouldReturnFormattedName()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        string result = _kisis.GetName(email);

        // Assert
        result.Should().Be("TEST test@example.com:  - ");
    }

    [Fact]
    public void GetName_WithNullDates_ShouldReturnFormattedName()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        string result = _kisis.GetName(email, null, null);

        // Assert
        result.Should().Be("TEST test@example.com:  - ");
    }

    [Fact]
    public async Task MakeGroupLink_WithEmailAndDates_ShouldNotThrow()
    {
        // Arrange
        string email = "test@example.com";
        DateTime validFrom = new(2024, 1, 1);
        DateTime validUntil = new(2024, 12, 31);

        // Act & Assert
        Func<Task> action = async () => await _kisis.MakeGroupLink(email, validFrom, validUntil);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MakeGroupLink_WithMatchedModel_ShouldNotThrow()
    {
        // Arrange
        WorkerModel worker = CreateTestWorker();
        MatchedModel matchedModel = new(worker, _mockConfig.Object);

        // Act & Assert
        Func<Task> action = async () => await _kisis.MakeGroupLink(matchedModel);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveGroupLink_WithValidId_ShouldNotThrow()
    {
        // Arrange
        int id = 123;

        // Act & Assert
        Func<Task> action = async () => await _kisis.RemoveGroupLink(id);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void KisisConfig_ShouldHaveCorrectProperties()
    {
        // Arrange
        KisisConfig config = new()
        {
            ApiToken = "test-token",
            GroupId = 456,
            NamePrefix = "PREFIX"
        };

        // Assert
        config.ApiToken.Should().Be("test-token");
        config.GroupId.Should().Be(456);
        config.NamePrefix.Should().Be("PREFIX");
    }

    [Fact]
    public void KisisConfig_WithDefaultValues_ShouldHaveEmptyStrings()
    {
        // Arrange
        KisisConfig config = new();

        // Assert
        config.ApiToken.Should().BeEmpty();
        config.NamePrefix.Should().BeEmpty();
        config.GroupId.Should().Be(0);
    }

    private WorkerModel CreateTestWorker()
    {
        DateTime now = DateTime.UtcNow;
        return new WorkerModel
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
                    InductedOnUtc = now.AddDays(-10),
                    ExpiresOnUtc = now.AddDays(10)
                }
            },
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "Test Contractor"
            },
            Contractor = new Contractor
            {
                ContractorID = 1,
                IsCompliant = true,
                Records =
                [
                    new() {
                        RecordID = 1,
                        ExpiresOnUtc = now.AddDays(20)
                    }
                ]
            }
        };
    }
}

// Helper class for testing JSON deserialization
public class KisisJsonTests
{
    [Fact]
    public void DeserializeGroupLinksModel_ShouldWorkCorrectly()
    {
        // Arrange
        string json = """
        [
            {
                "id": 1,
                "email": "test@example.com",
                "phone": null,
                "group_id": 123,
                "issued_by_id": 456,
                "name": "TEST John Test Contractor test@example.com: 2024-01-01 - 2024-12-31",
                "link_enabled": true,
                "quick_response_code_type": null,
                "valid_from": "2024-01-01T00:00:00Z",
                "valid_until": "2024-12-31T23:59:59Z",
                "last_used_at": "2024-01-15T10:30:00Z",
                "created_at": "2024-01-01T00:00:00Z",
                "updated_at": "2024-01-01T00:00:00Z",
                "issued_by": {
                    "id": 456,
                    "name": "Admin User",
                    "email": "admin@example.com"
                }
            }
        ]
        """;

        // Act
        List<GroupLinksModel>? result = JsonSerializer.Deserialize<List<GroupLinksModel>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].Email.Should().Be("test@example.com");
        result[0].GroupId.Should().Be(123);
        result[0].Name.Should().Be("TEST John Test Contractor test@example.com: 2024-01-01 - 2024-12-31");
        result[0].LinkEnabled.Should().BeTrue();
        result[0].ValidFrom.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result[0].ValidUntil.Should().Be(new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc));
        result[0].IssuedBy.Should().NotBeNull();
        result[0].IssuedBy!.Id.Should().Be(456);
        result[0].IssuedBy.Name.Should().Be("Admin User");
        result[0].IssuedBy.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public void DeserializeGroupLinkCreateModel_ShouldWorkCorrectly()
    {
        // Arrange
        string json = """
        {
            "group_link": {
                "email": "test@example.com",
                "group_id": 123,
                "name": "TEST John Test Contractor test@example.com: 2024-01-01 - 2024-12-31",
                "phone": "",
                "quick_response_code_type": "",
                "valid_from": "2024-01-01T00:00:00Z",
                "valid_until": "2024-12-31T23:59:59Z"
            }
        }
        """;

        // Act
        GroupLinkCreateModel? result = JsonSerializer.Deserialize<GroupLinkCreateModel>(json);

        // Assert
        result.Should().NotBeNull();
        result.GroupLink.Should().NotBeNull();
        result.GroupLink.Email.Should().Be("test@example.com");
        result.GroupLink.GroupId.Should().Be(123);
        result.GroupLink.Name.Should().Be("TEST John Test Contractor test@example.com: 2024-01-01 - 2024-12-31");
        result.GroupLink.ValidFrom.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.GroupLink.ValidUntil.Should().Be(new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public void DeserializeEmptyGroupLinksList_ShouldWorkCorrectly()
    {
        // Arrange
        string json = "[]";

        // Act
        List<GroupLinksModel>? result = JsonSerializer.Deserialize<List<GroupLinksModel>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

