using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Options;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class MatchedModelTests
{
    private readonly IOptions<KisisConfig> _config;

    public MatchedModelTests()
    {
        _config = Options.Create(new KisisConfig
        {
            NamePrefix = "TestPrefix",
            GroupId = 123,
            ApiToken = "test-token"
        });
    }

    [Fact]
    public void Constructor_WithValidWorker_ShouldCreateCompliantModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        var currentTime = DateTime.UtcNow;

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.EmailAddress.Should().Be("test@example.com");
        result.ValidFrom.Should().Be(worker.Inductions[0].InductedOnUtc);
        result.ValidTo.Should().Be(worker.Inductions[0].ExpiresOnUtc);
        result.IsCompliant.Should().BeTrue();
        result.WorkerModel.Should().Be(worker);
        result.KisiName.Should().Contain("TestPrefix");
        result.KisiName.Should().Contain("John");
        result.KisiName.Should().Contain("TestContractor");
        result.KisiName.Should().Contain("test@example.com");
    }

    [Fact]
    public void Constructor_WithNullWorker_ShouldThrowArgumentNullException()
    {
        // Arrange
        WorkerModel? worker = null;

        // Act & Assert
        var action = () => new MatchedModel(worker!, _config);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithWorkerWithoutContractor_ShouldThrowException()
    {
        // Arrange
        var worker = CreateValidWorker();
        worker.Contractor = null;

        // Act & Assert
        var action = () => new MatchedModel(worker, _config);
        action.Should().Throw<Exception>().WithMessage("The worker does not contain a contractor?");
    }

    [Fact]
    public void Constructor_WithExpiredInduction_ShouldCreateNonCompliantModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        worker.Inductions[0].ExpiresOnUtc = DateTime.UtcNow.AddDays(-1); // Expired yesterday

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.ValidFrom.Should().BeNull();
        result.ValidTo.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNonCompliantWorker_ShouldCreateNonCompliantModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        worker.IsCompliant = false;

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNonCompliantContractor_ShouldCreateNonCompliantModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        worker.Contractor!.IsCompliant = false;

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void CreateGroupLinkModel_ShouldReturnCorrectModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        var matchedModel = new MatchedModel(worker, _config);

        // Act
        var result = matchedModel.CreateGroupLinkModel(_config);

        // Assert
        result.GroupLink.Should().NotBeNull();
        result.GroupLink.Name.Should().Be(matchedModel.KisiName);
        result.GroupLink.Email.Should().Be(matchedModel.EmailAddress);
        result.GroupLink.GroupId.Should().Be(_config.Value.GroupId);
        result.GroupLink.ValidFrom.Should().Be(matchedModel.ValidFrom);
        result.GroupLink.ValidUntil.Should().Be(matchedModel.ValidTo);
    }

    [Fact]
    public void Constructor_WithFutureValidFrom_ShouldCreateNonCompliantModel()
    {
        // Arrange
        var worker = CreateValidWorker();
        worker.Inductions[0].InductedOnUtc = DateTime.UtcNow.AddDays(1); // Future induction

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithMultipleInductions_ShouldUseEarliestValid()
    {
        // Arrange
        var worker = CreateValidWorker();
        var currentTime = DateTime.UtcNow;
        
        // Add another induction that's valid but later
        worker.Inductions = worker.Inductions.Concat(new[]
        {
            new InductionModel
            {
                InductionId = 2,
                InductedOnUtc = currentTime.AddDays(-5),
                ExpiresOnUtc = currentTime.AddDays(10)
            }
        }).ToArray();

        // Act
        var result = new MatchedModel(worker, _config);

        // Assert
        result.ValidFrom.Should().Be(worker.Inductions[0].InductedOnUtc); // Should use the earliest valid induction
        result.IsCompliant.Should().BeTrue();
    }

    private WorkerModel CreateValidWorker()
    {
        var currentTime = DateTime.UtcNow;
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
                    InductedOnUtc = currentTime.AddDays(-10),
                    ExpiresOnUtc = currentTime.AddDays(10)
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
                        ExpiresOnUtc = currentTime.AddDays(15)
                    }
                }
            }
        };
    }
}
