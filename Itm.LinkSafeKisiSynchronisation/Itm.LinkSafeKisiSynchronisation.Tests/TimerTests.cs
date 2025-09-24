using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class TimerTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<Timer>> _mockLogger;
    private readonly Mock<ErrorService> _mockErrorService;
    private readonly Mock<LinkSafe> _mockLinkSafe;
    private readonly Mock<Kisis> _mockKisis;
    private readonly IOptions<KisisConfig> _config;
    private readonly Timer _timer;

    public TimerTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<Timer>>();
        _mockErrorService = new Mock<ErrorService>(Mock.Of<IOptions<EmailConfig>>());
        _mockLinkSafe = new Mock<LinkSafe>(Mock.Of<ErrorService>(), Mock.Of<IOptions<LinkSafeConfig>>(), Mock.Of<ILogger<LinkSafe>>());
        _mockKisis = new Mock<Kisis>(Mock.Of<ErrorService>(), Mock.Of<IOptions<KisisConfig>>(), Mock.Of<ILogger<Kisis>>());
        _config = Options.Create(new KisisConfig
        {
            ApiToken = "test-token",
            GroupId = 123,
            NamePrefix = "TestPrefix"
        });

        _mockLoggerFactory.Setup(x => x.CreateLogger<Timer>()).Returns(_mockLogger.Object);

        _timer = new Timer(
            _mockLoggerFactory.Object,
            _mockErrorService.Object,
            _mockLinkSafe.Object,
            _mockKisis.Object,
            _config
        );
    }

    [Fact]
    public async Task RunHttp_WithSuccessfulSynchronization_ShouldReturnOkResponse()
    {
        // Arrange
        var mockRequest = new Mock<HttpRequestData>(Mock.Of<FunctionContext>());
        var mockResponse = new Mock<HttpResponseData>(Mock.Of<FunctionContext>());
        var mockFunctionContext = new Mock<FunctionContext>();

        mockRequest.Setup(x => x.CreateResponse(HttpStatusCode.OK)).Returns(mockResponse.Object);
        mockResponse.Setup(x => x.Headers).Returns(new HttpHeadersCollection());

        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ReturnsAsync(CreateValidWorkers());
        _mockKisis.Setup(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupLinksModel>());

        // Act
        var result = await _timer.RunHttp(mockRequest.Object, mockFunctionContext.Object);

        // Assert
        result.Should().Be(mockResponse.Object);
        mockResponse.Verify(x => x.WriteStringAsync(It.Is<string>(s => s.Contains("Successfully completed"))), Times.Once);
    }

    [Fact]
    public async Task RunHttp_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var mockRequest = new Mock<HttpRequestData>(Mock.Of<FunctionContext>());
        var mockResponse = new Mock<HttpResponseData>(Mock.Of<FunctionContext>());
        var mockFunctionContext = new Mock<FunctionContext>();

        mockRequest.Setup(x => x.CreateResponse(HttpStatusCode.InternalServerError)).Returns(mockResponse.Object);
        mockResponse.Setup(x => x.Headers).Returns(new HttpHeadersCollection());

        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _timer.RunHttp(mockRequest.Object, mockFunctionContext.Object);

        // Assert
        result.Should().Be(mockResponse.Object);
        mockResponse.Verify(x => x.WriteStringAsync(It.Is<string>(s => s.Contains("An Error Occurred"))), Times.Once);
        _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldCallSynchronizeKisiAndLinksafe()
    {
        // Arrange
        var mockTimerInfo = new Mock<TimerInfo>();

        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ReturnsAsync(CreateValidWorkers());
        _mockKisis.Setup(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupLinksModel>());

        // Act
        await _timer.Run(mockTimerInfo.Object);

        // Assert
        _mockLinkSafe.Verify(x => x.MatchWorkersToTheirContractor(), Times.Once);
        _mockKisis.Verify(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchonizeKisiAndLinksafe_WithNoWorkers_ShouldReturnZeroCounts()
    {
        // Arrange
        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ReturnsAsync(Array.Empty<WorkerModel>());
        _mockKisis.Setup(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupLinksModel>());

        // Act
        var result = await _timer.SynchonizeKisiAndLinksafe();

        // Assert
        result.Should().Be((0, 0, 0));
    }

    [Fact]
    public async Task SynchonizeKisiAndLinksafe_WithCompliantWorkers_ShouldAddWorkers()
    {
        // Arrange
        var workers = CreateValidWorkers();
        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ReturnsAsync(workers);
        _mockKisis.Setup(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupLinksModel>());

        // Act
        var result = await _timer.SynchonizeKisiAndLinksafe();

        // Assert
        result.added.Should().Be(1); // One compliant worker
        result.updated.Should().Be(0);
        result.deleted.Should().Be(0);
        _mockKisis.Verify(x => x.MakeGroupLink(It.IsAny<MatchedModel>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchonizeKisiAndLinksafe_WithNonCompliantWorkers_ShouldRemoveWorkers()
    {
        // Arrange
        var workers = CreateNonCompliantWorkers();
        var existingGroupLinks = new List<GroupLinksModel>
        {
            new() { Id = 1, Email = "test@example.com", Name = "Test: 2023-01-01 - 2023-12-31" }
        };

        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ReturnsAsync(workers);
        _mockKisis.Setup(x => x.GetGroupLinks(0, It.IsAny<List<GroupLinksModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroupLinks);

        // Act
        var result = await _timer.SynchonizeKisiAndLinksafe();

        // Assert
        result.added.Should().Be(0);
        result.updated.Should().Be(0);
        result.deleted.Should().Be(1); // One non-compliant worker removed
        _mockKisis.Verify(x => x.RemoveGroupLink(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynchonizeKisiAndLinksafe_WithException_ShouldLogErrorAndThrow()
    {
        // Arrange
        _mockLinkSafe.Setup(x => x.MatchWorkersToTheirContractor())
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var action = async () => await _timer.SynchonizeKisiAndLinksafe();
        await action.Should().ThrowAsync<Exception>().WithMessage("Test exception");
        
        _mockErrorService.Verify(x => x.AddErrorLog("Test exception"), Times.Once);
        _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), "An error occurred during synchronization."), Times.Once);
    }

    private WorkerModel[] CreateValidWorkers()
    {
        var currentTime = DateTime.UtcNow;
        return new[]
        {
            new WorkerModel
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
            }
        };
    }

    private WorkerModel[] CreateNonCompliantWorkers()
    {
        var currentTime = DateTime.UtcNow;
        return new[]
        {
            new WorkerModel
            {
                WorkerId = 1,
                EmailAddress = "test@example.com",
                FirstName = "John",
                IsCompliant = false, // Non-compliant worker
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
            }
        };
    }
}
