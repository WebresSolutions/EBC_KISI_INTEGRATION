using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Record = Itm.LinkSafeKisiSynchronisation.LinkSafeModels.Record;

namespace SyncTests;

/// <summary>
/// Integration tests that test the complete synchronization flow with real data structures
/// but mocked external dependencies
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void MatchedModel_WithRealWorkerData_ShouldCreateCorrectKisiName()
    {
        // Arrange
        IOptions<KisisConfig> config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "LINKSAFE"
        });

        WorkerModel worker = CreateRealWorkerModel();
        MatchedModel matchedModel = new(worker, config);

        // Act
        GroupLinkCreateModel groupLinkModel = matchedModel.CreateGroupLinkModel(config);

        // Assert
        groupLinkModel.Should().NotBeNull();
        groupLinkModel.GroupLink.Should().NotBeNull();
        groupLinkModel.GroupLink.Email.Should().Be("john.doe@contractor.com");
        groupLinkModel.GroupLink.GroupId.Should().Be(123);
        groupLinkModel.GroupLink.Name.Should().Contain("LINKSAFE");
        groupLinkModel.GroupLink.Name.Should().Contain("John");
        groupLinkModel.GroupLink.Name.Should().Contain("ABC Construction");
        groupLinkModel.GroupLink.Name.Should().Contain("john.doe@contractor.com");
        groupLinkModel.GroupLink.ValidFrom.Should().NotBeNull();
        groupLinkModel.GroupLink.ValidUntil.Should().NotBeNull();
    }

    [Fact]
    public void MatchedModel_WithExpiredInduction_ShouldBeNonCompliant()
    {
        // Arrange
        IOptions<KisisConfig> config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "LINKSAFE"
        });

        WorkerModel worker = CreateExpiredWorkerModel();
        MatchedModel matchedModel = new(worker, config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
        matchedModel.ValidFrom.Should().BeNull();
        matchedModel.ValidTo.Should().BeNull();
        matchedModel.WorkerModel.Should().BeNull();
    }

    [Fact]
    public void MatchedModel_WithFutureInduction_ShouldBeNonCompliant()
    {
        // Arrange
        IOptions<KisisConfig> config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "LINKSAFE"
        });

        WorkerModel worker = CreateFutureInductionWorkerModel();
        MatchedModel matchedModel = new(worker, config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
        matchedModel.ValidFrom.Should().BeNull();
        matchedModel.ValidTo.Should().BeNull();
        matchedModel.WorkerModel.Should().BeNull();
    }

    [Fact]
    public void MatchedModel_WithNonCompliantContractor_ShouldBeNonCompliant()
    {
        // Arrange
        IOptions<KisisConfig> config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "LINKSAFE"
        });

        WorkerModel worker = CreateNonCompliantContractorWorkerModel();
        MatchedModel matchedModel = new(worker, config);

        // Assert
        matchedModel.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void StaticHelpers_AreDatesEqualIgnoringTimeZone_WithRealDates_ShouldWorkCorrectly()
    {
        // Arrange
        DateTime utcDate = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        DateTime localDate = new(2024, 1, 15, 20, 0, 0, DateTimeKind.Local); // Different timezone

        // Act
        bool result = StaticHelpers.AreDatesEqualIgnoringTimeZone(utcDate, localDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void StaticHelpers_RunWithRateLimit_WithRealTasks_ShouldRespectBatching()
    {
        // Arrange
        int taskCount = 0;
        List<Task> tasks = new();
        for (int i = 0; i < 7; i++)
        {
            tasks.Add(Task.Run(() => { Interlocked.Increment(ref taskCount); }));
        }

        // Act
        DateTime startTime = DateTime.UtcNow;
        StaticHelpers.RunWithRateLimit(tasks, batchSize: 3, delayMs: 50).Wait();
        DateTime endTime = DateTime.UtcNow;

        // Assert
        taskCount.Should().Be(7);
        (endTime - startTime).TotalMilliseconds.Should().BeGreaterOrEqualTo(100); // 2 delays of 50ms each
    }

    [Fact]
    public void JsonSerialization_WithRealData_ShouldWorkCorrectly()
    {
        // Arrange
        WorkersModel workersModel = new()
        {
            Workers = new[]
            {
                CreateRealWorkerModel(),
                CreateExpiredWorkerModel()
            }
        };

        // Act
        string json = JsonSerializer.Serialize(workersModel);
        WorkersModel? deserialized = JsonSerializer.Deserialize<WorkersModel>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Workers.Should().HaveCount(2);
        deserialized.Workers[0].EmailAddress.Should().Be("john.doe@contractor.com");
        deserialized.Workers[1].EmailAddress.Should().Be("jane.smith@contractor.com");
    }

    [Fact]
    public void JsonSerialization_WithKisiData_ShouldWorkCorrectly()
    {
        // Arrange
        List<GroupLinksModel> groupLinks = new()
        {
            new GroupLinksModel
            {
                Id = 1,
                Email = "worker@example.com",
                Name = "LINKSAFE John ABC Construction worker@example.com: 2024-01-01 - 2024-12-31",
                GroupId = 123,
                ValidFrom = DateTime.UtcNow.AddDays(-10),
                ValidUntil = DateTime.UtcNow.AddDays(10),
                LinkEnabled = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                IssuedBy = new IssuedBy
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@example.com"
                }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(groupLinks);
        List<GroupLinksModel>? deserialized = JsonSerializer.Deserialize<List<GroupLinksModel>>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(1);
        deserialized[0].Id.Should().Be(1);
        deserialized[0].Email.Should().Be("worker@example.com");
        deserialized[0].Name.Should().Be("LINKSAFE John ABC Construction worker@example.com: 2024-01-01 - 2024-12-31");
        deserialized[0].GroupId.Should().Be(123);
        deserialized[0].IssuedBy.Should().NotBeNull();
        deserialized[0].IssuedBy!.Name.Should().Be("Admin User");
    }

    [Fact]
    public void ErrorService_WithRealConfiguration_ShouldInitializeCorrectly()
    {
        // Arrange
        IOptions<EmailConfig> emailConfig = Options.Create(new EmailConfig
        {
            Smtp = "smtp.gmail.com",
            Username = "test@gmail.com",
            Password = "testpassword"
        });

        // Act
        ErrorService errorService = new(emailConfig);

        // Assert
        errorService.Should().NotBeNull();
        errorService.Content.Should().BeEmpty();
    }

    [Fact]
    public void ErrorService_WithMultipleErrors_ShouldMaintainOrder()
    {
        // Arrange
        IOptions<EmailConfig> emailConfig = Options.Create(new EmailConfig
        {
            Smtp = "smtp.gmail.com",
            Username = "test@gmail.com",
            Password = "testpassword"
        });
        ErrorService errorService = new(emailConfig);

        // Act
        errorService.AddErrorLog("First error");
        errorService.AddErrorLog("Second error");
        errorService.AddErrorLog("Third error");

        // Assert
        errorService.Content.Should().HaveCount(3);
        errorService.Content[0].Message.Should().Be("First error");
        errorService.Content[1].Message.Should().Be("Second error");
        errorService.Content[2].Message.Should().Be("Third error");
    }

    private WorkerModel CreateRealWorkerModel()
    {
        DateTime now = DateTime.UtcNow;
        return new WorkerModel
        {
            WorkerId = 1,
            EmailAddress = "john.doe@contractor.com",
            FirstName = "John",
            IsCompliant = true,
            Inductions = new[]
            {
                new InductionModel
                {
                    InductionId = 1,
                    InductedOnUtc = now.AddDays(-30),
                    ExpiresOnUtc = now.AddDays(30)
                },
                new InductionModel
                {
                    InductionId = 2,
                    InductedOnUtc = now.AddDays(-15),
                    ExpiresOnUtc = now.AddDays(45)
                }
            },
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "ABC Construction"
            },
            Contractor = new Contractor
            {
                ContractorID = 1,
                IsCompliant = true,
                Status = "Active",
                StateCode = "NSW",
                Country = "Australia",
                Postcode = "2000",
                Phone = "02 1234 5678",
                Records = new List<Record>
                {
                    new() {
                        RecordID = 1,
                        RecordType = "Public Liability Insurance",
                        RecordTypeID = 1,
                        ExpiryType = "Annual",
                        Reference = "PLI-2024-001",
                        Description = "Public Liability Insurance - $20M",
                        ExpiresOnUtc = now.AddDays(60),
                        RecordStatus = "Valid",
                        CreatedOnUtc = now.AddDays(-90)
                    }
                }
            }
        };
    }

    private static WorkerModel CreateExpiredWorkerModel()
    {
        DateTime now = DateTime.UtcNow;
        return new WorkerModel
        {
            WorkerId = 2,
            EmailAddress = "jane.smith@contractor.com",
            FirstName = "Jane",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 3,
                    InductedOnUtc = now.AddDays(-60),
                    ExpiresOnUtc = now.AddDays(-5) // Expired
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "ABC Construction"
            },
            Contractor = new Contractor
            {
                ContractorID = 1,
                IsCompliant = true,
                Records = []
            }
        };
    }

    private static WorkerModel CreateFutureInductionWorkerModel()
    {
        DateTime now = DateTime.UtcNow;
        return new WorkerModel
        {
            WorkerId = 3,
            EmailAddress = "bob.wilson@contractor.com",
            FirstName = "Bob",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 4,
                    InductedOnUtc = now.AddDays(5), // Future date
                    ExpiresOnUtc = now.AddDays(35)
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 1,
                DisplayName = "ABC Construction"
            },
            Contractor = new Contractor
            {
                ContractorID = 1,
                IsCompliant = true,
                Records = []
            }
        };
    }

    private static WorkerModel CreateNonCompliantContractorWorkerModel()
    {
        DateTime now = DateTime.UtcNow;
        return new WorkerModel
        {
            WorkerId = 4,
            EmailAddress = "alice.brown@contractor.com",
            FirstName = "Alice",
            IsCompliant = true,
            Inductions =
            [
                new InductionModel
                {
                    InductionId = 5,
                    InductedOnUtc = now.AddDays(-10),
                    ExpiresOnUtc = now.AddDays(20)
                }
            ],
            PrimaryContractor = new PrimaryContractor
            {
                ContactorID = 2,
                DisplayName = "XYZ Construction"
            },
            Contractor = new Contractor
            {
                ContractorID = 2,
                IsCompliant = false, // Non-compliant contractor
                Status = "Suspended",
                Records = []
            }
        };
    }
}

