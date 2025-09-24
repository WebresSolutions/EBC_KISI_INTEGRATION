using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class TimerPrivateMethodTests
{
    [Fact]
    public void GrooupLinkNeedsToBeUpdated_WithDifferentNames_ShouldReturnTrue()
    {
        // Arrange
        var matchedModel = CreateMatchedModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        var kisiModel = CreateKisiModel("DifferentPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");

        // Act
        var result = CallPrivateMethod(matchedModel, kisiModel);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GrooupLinkNeedsToBeUpdated_WithSameNames_ShouldReturnFalse()
    {
        // Arrange
        var matchedModel = CreateMatchedModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        var kisiModel = CreateKisiModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");

        // Act
        var result = CallPrivateMethod(matchedModel, kisiModel);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GrooupLinkNeedsToBeUpdated_WithDifferentValidFrom_ShouldReturnTrue()
    {
        // Arrange
        var matchedModel = CreateMatchedModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        var kisiModel = CreateKisiModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        matchedModel = CreateMatchedModelWithDates(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30));

        // Act
        var result = CallPrivateMethod(matchedModel, kisiModel);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GrooupLinkNeedsToBeUpdated_WithDifferentValidUntil_ShouldReturnTrue()
    {
        // Arrange
        var matchedModel = CreateMatchedModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        var kisiModel = CreateKisiModel("TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31");
        matchedModel = CreateMatchedModelWithDates(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(1));

        // Act
        var result = CallPrivateMethod(matchedModel, kisiModel);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GrooupLinkNeedsToBeUpdated_WithSameDates_ShouldReturnFalse()
    {
        // Arrange
        var validFrom = DateTime.UtcNow.AddDays(-10);
        var validUntil = DateTime.UtcNow.AddDays(30);
        var matchedModel = CreateMatchedModelWithDates(validFrom, validUntil);
        var kisiModel = CreateKisiModelWithDates(validFrom, validUntil);

        // Act
        var result = CallPrivateMethod(matchedModel, kisiModel);

        // Assert
        result.Should().BeFalse();
    }

    private bool CallPrivateMethod(MatchedModel matchedModel, GroupLinksModel kisiModel)
    {
        var method = typeof(Timer).GetMethod("GrooupLinkNeedsToBeUpdated", BindingFlags.NonPublic | BindingFlags.Static);
        return (bool)method!.Invoke(null, new object[] { matchedModel, kisiModel })!;
    }

    private MatchedModel CreateMatchedModel(string kisiName)
    {
        var config = Options.Create(new KisisConfig
        {
            NamePrefix = "TestPrefix",
            GroupId = 123,
            ApiToken = "test-token"
        });

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
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(30)
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
                        ExpiresOnUtc = DateTime.UtcNow.AddDays(30)
                    }
                }
            }
        };

        var matchedModel = new MatchedModel(worker, config);
        
        // Use reflection to set the KisiName property
        var kisiNameField = typeof(MatchedModel).GetField("KisiName", BindingFlags.NonPublic | BindingFlags.Instance);
        kisiNameField?.SetValue(matchedModel, kisiName);

        return matchedModel;
    }

    private MatchedModel CreateMatchedModelWithDates(DateTime? validFrom, DateTime? validTo)
    {
        var config = Options.Create(new KisisConfig
        {
            NamePrefix = "TestPrefix",
            GroupId = 123,
            ApiToken = "test-token"
        });

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
                    InductedOnUtc = validFrom ?? DateTime.UtcNow.AddDays(-10),
                    ExpiresOnUtc = validTo ?? DateTime.UtcNow.AddDays(30)
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
                        ExpiresOnUtc = validTo ?? DateTime.UtcNow.AddDays(30)
                    }
                }
            }
        };

        return new MatchedModel(worker, config);
    }

    private GroupLinksModel CreateKisiModel(string name)
    {
        return new GroupLinksModel
        {
            Id = 1,
            Email = "test@example.com",
            Name = name,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };
    }

    private GroupLinksModel CreateKisiModelWithDates(DateTime? validFrom, DateTime? validUntil)
    {
        return new GroupLinksModel
        {
            Id = 1,
            Email = "test@example.com",
            Name = "TestPrefix John TestContractor test@example.com: 2023-01-01 - 2023-12-31",
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }
}
