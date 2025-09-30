using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Options;
using Record = Itm.LinkSafeKisiSynchronisation.LinkSafeModels.Record;

namespace SyncTests;

public class MatchedModelTests
{
    private readonly IOptions<KisisConfig> _config;

    public MatchedModelTests()
    {
        KisisConfig config = new()
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "TEST"
        };
        _config = Options.Create(config);
    }

    [Fact]
    public void Constructor_WhenWorkerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MatchedModel(null!, _config));
    }

    [Fact]
    public void Constructor_WhenWorkerHasNoContractor_ShouldThrowException()
    {
        // Arrange
        WorkerModel worker = new()
        {
            WorkerId = 1,
            EmailAddress = "test@example.com",
            FirstName = "John",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 1,
                    InductedOnUtc = DateTime.UtcNow.AddDays(-10),
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(10)
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "Test Contractor"
            }
        };

        // Act & Assert
        Exception exception = Assert.Throws<Exception>(() => new MatchedModel(worker, _config));
        exception.Message.Should().Be("The worker does not contain a contractor?");
    }

    [Fact]
    public void Constructor_WhenWorkerIsCompliant_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.EmailAddress.Should().Be("test@example.com");
        matchedModel.WorkerModel.Should().Be(worker);
        matchedModel.IsCompliant.Should().BeTrue();
        matchedModel.ValidFrom.Should().BeCloseTo(now.AddDays(-10), TimeSpan.FromMinutes(1));
        matchedModel.ValidTo.Should().BeCloseTo(now.AddDays(10), TimeSpan.FromMinutes(1));
        matchedModel.KisiName.Should().Contain("TEST John Test Contractor test@example.com");
    }

    [Fact]
    public void Constructor_WhenWorkerHasNoValidInductions_ShouldSetNonCompliant()
    {
        // Arrange
        WorkerModel worker = new()
        {
            WorkerId = 1,
            EmailAddress = "test@example.com",
            FirstName = "John",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 1,
                    InductedOnUtc = DateTime.UtcNow.AddDays(-20),
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(-5) // Expired
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "Test Contractor"
            },
            Contractor = CreateCompliantContractor()
        };

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
        matchedModel.ValidFrom.Should().BeNull();
        matchedModel.ValidTo.Should().BeNull();
        matchedModel.WorkerModel.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenContractorIsNotCompliant_ShouldSetNonCompliant()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        worker.Contractor!.IsCompliant = false;

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenValidFromIsInFuture_ShouldSetNonCompliant()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        worker.Inductions = new[]
        {
            new InductionModel
            {
                InductionId = 1,
                InductedOnUtc = now.AddDays(5), // Future date
                ExpiresOnUtc = now.AddDays(15)
            }
        };

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenValidToIsInPast_ShouldSetNonCompliant()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        worker.Inductions = new[]
        {
            new InductionModel
            {
                InductionId = 1,
                InductedOnUtc = now.AddDays(-20),
                ExpiresOnUtc = now.AddDays(-5) // Past date
            }
        };

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void CreateGroupLinkModel_ShouldReturnCorrectModel()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        MatchedModel matchedModel = new(worker, _config);

        // Act
        GroupLinkCreateModel groupLinkModel = matchedModel.CreateGroupLinkModel(_config);

        // Assert
        groupLinkModel.Should().NotBeNull();
        groupLinkModel.GroupLink.Should().NotBeNull();
        groupLinkModel.GroupLink.Email.Should().Be("test@example.com");
        groupLinkModel.GroupLink.GroupId.Should().Be(123);
        groupLinkModel.GroupLink.Name.Should().Be(matchedModel.KisiName);
        groupLinkModel.GroupLink.ValidFrom.Should().Be(matchedModel.ValidFrom);
        groupLinkModel.GroupLink.ValidUntil.Should().Be(matchedModel.ValidTo);
    }

    [Fact]
    public void Constructor_WhenMultipleInductions_ShouldUseEarliestValidInduction()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        worker.Inductions = new[]
        {
            new InductionModel
            {
                InductionId = 1,
                InductedOnUtc = now.AddDays(-5),
                ExpiresOnUtc = now.AddDays(5)
            },
            new InductionModel
            {
                InductionId = 2,
                InductedOnUtc = now.AddDays(-10), // Earlier
                ExpiresOnUtc = now.AddDays(10)
            }
        };

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.ValidFrom.Should().BeCloseTo(now.AddDays(-10), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Constructor_WhenMultipleInductions_ShouldUseLatestExpiry()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        WorkerModel worker = CreateCompliantWorker(now);
        worker.Inductions = new[]
        {
            new InductionModel
            {
                InductionId = 1,
                InductedOnUtc = now.AddDays(-10),
                ExpiresOnUtc = now.AddDays(5)
            },
            new InductionModel
            {
                InductionId = 2,
                InductedOnUtc = now.AddDays(-8),
                ExpiresOnUtc = now.AddDays(15) // Later
            }
        };

        // Act
        MatchedModel matchedModel = new(worker, _config);

        // Assert
        matchedModel.ValidTo.Should().BeCloseTo(now.AddDays(15), TimeSpan.FromMinutes(1));
    }

    private static WorkerModel CreateCompliantWorker(DateTime now)
    {
        return new WorkerModel
        {
            WorkerId = 1,
            EmailAddress = "test@example.com",
            FirstName = "John",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 1,
                    InductedOnUtc = now.AddDays(-10),
                    ExpiresOnUtc = now.AddDays(10)
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "Test Contractor"
            },
            Contractor = CreateCompliantContractor()
        };
    }

    private static Contractor CreateCompliantContractor()
    {
        DateTime now = DateTime.UtcNow;
        return new Contractor
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
        };
    }
}

