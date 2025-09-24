using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;
using Microsoft.Extensions.Options;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class ErrorServiceTests
{
    [Fact]
    public void AddErrorLog_WithMessage_ShouldAddToContent()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });
        var errorService = new ErrorService(config);
        var message = "Test error message";

        // Act
        errorService.AddErrorLog(message);

        // Assert
        errorService.Content.Should().HaveCount(1);
        errorService.Content[0].Message.Should().Be(message);
        errorService.Content[0].TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddErrorLog_WithCustomTimestamp_ShouldUseProvidedTimestamp()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });
        var errorService = new ErrorService(config);
        var message = "Test error message";
        var customTimestamp = DateTime.UtcNow.AddHours(-1);

        // Act
        errorService.AddErrorLog(message, customTimestamp);

        // Assert
        errorService.Content.Should().HaveCount(1);
        errorService.Content[0].Message.Should().Be(message);
        errorService.Content[0].TimeStamp.Should().Be(customTimestamp);
    }

    [Fact]
    public void AddErrorLog_MultipleMessages_ShouldAddAllToContent()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });
        var errorService = new ErrorService(config);

        // Act
        errorService.AddErrorLog("Error 1");
        errorService.AddErrorLog("Error 2");
        errorService.AddErrorLog("Error 3");

        // Assert
        errorService.Content.Should().HaveCount(3);
        errorService.Content[0].Message.Should().Be("Error 1");
        errorService.Content[1].Message.Should().Be("Error 2");
        errorService.Content[2].Message.Should().Be("Error 3");
    }

    [Fact]
    public void Content_Initially_ShouldBeEmpty()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });

        // Act
        var errorService = new ErrorService(config);

        // Assert
        errorService.Content.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_WithEmptyContent_ShouldNotThrow()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });
        var errorService = new ErrorService(config);

        // Act & Assert
        var action = () => errorService.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithContent_ShouldNotThrow()
    {
        // Arrange
        var config = Options.Create(new EmailConfig
        {
            Smtp = "smtp.test.com",
            Username = "test@test.com",
            Password = "password"
        });
        var errorService = new ErrorService(config);
        errorService.AddErrorLog("Test error");

        // Act & Assert
        var action = () => errorService.Dispose();
        action.Should().NotThrow();
    }
}
