using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;

namespace Itm.LinkSafeKisiSynchronisation.Tests;

public class StaticHelpersTests
{
    [Theory]
    [InlineData("2023-01-01T10:00:00Z", "2023-01-01T15:00:00Z", true)] // Same date, different times
    [InlineData("2023-01-01T10:00:00Z", "2023-01-02T10:00:00Z", true)] // 1 day difference
    [InlineData("2023-01-01T10:00:00Z", "2023-01-03T10:00:00Z", false)] // 2 days difference
    [InlineData("2023-01-01T10:00:00Z", "2023-01-01T10:00:00Z", true)] // Exact same date
    [InlineData(null, null, true)] // Both null
    [InlineData("2023-01-01T10:00:00Z", null, false)] // One null
    [InlineData(null, "2023-01-01T10:00:00Z", false)] // One null
    public void AreDatesEqualIgnoringTimeZone_ShouldReturnExpectedResult(string? dateA, string? dateB, bool expected)
    {
        // Arrange
        var dateTimeA = dateA != null ? DateTime.Parse(dateA) : (DateTime?)null;
        var dateTimeB = dateB != null ? DateTime.Parse(dateB) : (DateTime?)null;

        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(dateTimeA, dateTimeB);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task RunWithRateLimit_ShouldExecuteAllTasks()
    {
        // Arrange
        var executedTasks = new List<int>();
        var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(() => executedTasks.Add(i))).ToList();

        // Act
        await StaticHelpers.RunWithRateLimit(tasks, batchSize: 3, delayMs: 10);

        // Assert
        executedTasks.Should().HaveCount(10);
        executedTasks.Should().Contain(Enumerable.Range(1, 10));
    }

    [Fact]
    public async Task RunWithRateLimit_WithEmptyList_ShouldCompleteImmediately()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        (endTime - startTime).TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task RunWithRateLimit_WithSingleTask_ShouldExecuteWithoutDelay()
    {
        // Arrange
        var executed = false;
        var task = Task.Run(() => executed = true);

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(new[] { task }, batchSize: 1, delayMs: 1000);
        var endTime = DateTime.UtcNow;

        // Assert
        executed.Should().BeTrue();
        (endTime - startTime).TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task RunWithRateLimit_WithMultipleBatches_ShouldRespectDelay()
    {
        // Arrange
        var executedTasks = new List<int>();
        var tasks = Enumerable.Range(1, 7).Select(i => Task.Run(() => executedTasks.Add(i))).ToList();

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(tasks, batchSize: 3, delayMs: 100);
        var endTime = DateTime.UtcNow;

        // Assert
        executedTasks.Should().HaveCount(7);
        // Should take at least 100ms due to delay between batches (3 tasks, then 3 tasks, then 1 task = 2 delays)
        (endTime - startTime).TotalMilliseconds.Should().BeGreaterOrEqualTo(200);
    }
}
