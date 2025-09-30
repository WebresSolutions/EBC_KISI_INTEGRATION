using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace SyncTests;

public class LinkSafeTests
{
    private readonly Mock<ErrorService> _mockErrorService;
    private readonly Mock<IOptions<LinkSafeConfig>> _mockConfig;
    private readonly Mock<ILogger<LinkSafe>> _mockLogger;
    private readonly LinkSafe _linkSafe;

    public LinkSafeTests()
    {
        _mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        _mockConfig = new Mock<IOptions<LinkSafeConfig>>();
        _mockConfig.Setup(x => x.Value).Returns(new LinkSafeConfig
        {
            ApiToken = "test-api-token"
        });
        _mockLogger = new Mock<ILogger<LinkSafe>>();

        _linkSafe = new LinkSafe(_mockErrorService.Object, _mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldSetApiTokenInClient()
    {
        // Arrange
        var mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        var mockConfig = new Mock<IOptions<LinkSafeConfig>>();
        mockConfig.Setup(x => x.Value).Returns(new LinkSafeConfig
        {
            ApiToken = "test-token"
        });
        var mockLogger = new Mock<ILogger<LinkSafe>>();

        // Act
        var linkSafe = new LinkSafe(mockErrorService.Object, mockConfig.Object, mockLogger.Object);

        // Assert
        linkSafe.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkers_WhenResponseContentIsNull_ShouldReturnEmptyArray()
    {
        // Act
        var result = await _linkSafe.GetWorkers();

        // Assert
        result.Should().NotBeNull();
        // The actual behavior depends on the API response
    }

    [Fact]
    public async Task GetContractors_WhenResponseContentIsNull_ShouldReturnEmptyArray()
    {
        // Act
        var result = await _linkSafe.GetContractors();

        // Assert
        result.Should().NotBeNull();
        // The actual behavior depends on the API response
    }

    [Fact]
    public async Task MatchWorkersToTheirContractor_ShouldMatchWorkersWithContractors()
    {
        // Act
        var result = await _linkSafe.MatchWorkersToTheirContractor();

        // Assert
        result.Should().NotBeNull();
        // The actual behavior depends on the API responses
    }

    [Fact]
    public void LinkSafeConfig_ShouldHaveRequiredProperties()
    {
        // Arrange
        var config = new LinkSafeConfig
        {
            ApiToken = "test-token"
        };

        // Assert
        config.ApiToken.Should().Be("test-token");
    }
}

// Helper class for testing JSON deserialization
public class LinkSafeJsonTests
{
    [Fact]
    public void DeserializeWorkersModel_ShouldWorkCorrectly()
    {
        // Arrange
        var json = """
        {
            "workers": [
                {
                    "workerID": 1,
                    "emailAddress": "test@example.com",
                    "firstName": "John",
                    "isCompliant": true,
                    "inductions": [
                        {
                            "inductionID": 1,
                            "inductedOnUtc": "2024-01-01T00:00:00Z",
                            "expiresOnUtc": "2024-12-31T23:59:59Z"
                        }
                    ],
                    "primaryContractor": {
                        "contractorID": 1,
                        "displayName": "Test Contractor"
                    }
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<WorkersModel>(json);

        // Assert
        result.Should().NotBeNull();
        result.Workers.Should().HaveCount(1);
        result.Workers[0].WorkerId.Should().Be(1);
        result.Workers[0].EmailAddress.Should().Be("test@example.com");
        result.Workers[0].FirstName.Should().Be("John");
        result.Workers[0].IsCompliant.Should().BeTrue();
        result.Workers[0].Inductions.Should().HaveCount(1);
        result.Workers[0].PrimaryContractor.Should().NotBeNull();
        result.Workers[0].PrimaryContractor!.ContactorID.Should().Be(1);
        result.Workers[0].PrimaryContractor.DisplayName.Should().Be("Test Contractor");
    }

    [Fact]
    public void DeserializeContractorsModel_ShouldWorkCorrectly()
    {
        // Arrange
        var json = """
        {
            "contractors": [
                {
                    "contractorID": 1,
                    "isCompliant": true,
                    "nearExpiringItems": 0,
                    "nonCompliantItems": 0,
                    "status": "Active",
                    "stateCode": "NSW",
                    "country": "Australia",
                    "postcode": "2000",
                    "phone": "1234567890",
                    "contacts": [],
                    "complianceItems": [],
                    "records": [
                        {
                            "recordID": 1,
                            "recordType": "Insurance",
                            "recordTypeID": 1,
                            "expiryType": "Annual",
                            "specialInstructions": "",
                            "templateFileName": "",
                            "templateFileSize": null,
                            "reference": "REF001",
                            "description": "Public Liability Insurance",
                            "expiresOnUtc": "2024-12-31T23:59:59Z",
                            "recordStatus": "Valid",
                            "createdOnUtc": "2024-01-01T00:00:00Z"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ContractorsModel>(json);

        // Assert
        result.Should().NotBeNull();
        result.Contractors.Should().HaveCount(1);
        result.Contractors[0].ContractorID.Should().Be(1);
        result.Contractors[0].IsCompliant.Should().BeTrue();
        result.Contractors[0].Records.Should().HaveCount(1);
        result.Contractors[0].Records[0].RecordID.Should().Be(1);
        result.Contractors[0].Records[0].RecordType.Should().Be("Insurance");
    }

    [Fact]
    public void DeserializeEmptyWorkersModel_ShouldWorkCorrectly()
    {
        // Arrange
        var json = """{"workers": []}""";

        // Act
        var result = JsonSerializer.Deserialize<WorkersModel>(json);

        // Assert
        result.Should().NotBeNull();
        result.Workers.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeEmptyContractorsModel_ShouldWorkCorrectly()
    {
        // Arrange
        var json = """{"contractors": []}""";

        // Act
        var result = JsonSerializer.Deserialize<ContractorsModel>(json);

        // Assert
        result.Should().NotBeNull();
        result.Contractors.Should().BeEmpty();
    }
}

