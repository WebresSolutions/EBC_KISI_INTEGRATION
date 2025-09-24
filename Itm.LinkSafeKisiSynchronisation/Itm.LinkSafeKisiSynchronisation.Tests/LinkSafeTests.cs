using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class LinkSafeTests
{
    private readonly Mock<ErrorService> _mockErrorService;
    private readonly Mock<ILogger<LinkSafe>> _mockLogger;
    private readonly IOptions<LinkSafeConfig> _config;
    private readonly LinkSafe _linkSafe;

    public LinkSafeTests()
    {
        _mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        _mockLogger = new Mock<ILogger<LinkSafe>>();
        _config = Options.Create(new LinkSafeConfig
        {
            ApiToken = "test-token"
        });
        _linkSafe = new LinkSafe(_mockErrorService.Object, _config, _mockLogger.Object);
    }

    [Fact]
    public async Task GetWorkers_ShouldNotThrow()
    {
        // Act & Assert
        var action = async () => await _linkSafe.GetWorkers();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetContractors_ShouldNotThrow()
    {
        // Act & Assert
        var action = async () => await _linkSafe.GetContractors();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MatchWorkersToTheirContractor_ShouldNotThrow()
    {
        // Act & Assert
        var action = async () => await _linkSafe.MatchWorkersToTheirContractor();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetWorkers_ShouldReturnArray()
    {
        // Act
        var result = await _linkSafe.GetWorkers();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<WorkerModel[]>();
    }

    [Fact]
    public async Task GetContractors_ShouldReturnArray()
    {
        // Act
        var result = await _linkSafe.GetContractors();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Contractor[]>();
    }

    [Fact]
    public async Task MatchWorkersToTheirContractor_ShouldReturnArray()
    {
        // Act
        var result = await _linkSafe.MatchWorkersToTheirContractor();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<WorkerModel[]>();
    }
}
