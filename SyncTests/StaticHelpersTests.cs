using FluentAssertions;
using Itm.LinkSafeKisiSynchronisation;

namespace SyncTests;

public class StaticHelpersTests
{
    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenBothDatesAreNull_ShouldReturnTrue()
    {
        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(null, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenOneDateIsNull_ShouldReturnFalse()
    {
        // Arrange
        var date1 = DateTime.UtcNow;
        DateTime? date2 = null;

        // Act
        var result1 = StaticHelpers.AreDatesEqualIgnoringTimeZone(date1, date2);
        var result2 = StaticHelpers.AreDatesEqualIgnoringTimeZone(date2, date1);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenDatesAreSame_ShouldReturnTrue()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15, 10, 30, 0);
        var date2 = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(date1, date2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenDatesAreSameDay_ShouldReturnTrue()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15, 10, 30, 0);
        var date2 = new DateTime(2024, 1, 15, 14, 45, 0);

        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(date1, date2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenDatesAreOneDayApart_ShouldReturnTrue()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15, 23, 59, 59);
        var date2 = new DateTime(2024, 1, 16, 0, 0, 1);

        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(date1, date2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreDatesEqualIgnoringTimeZone_WhenDatesAreMoreThanOneDayApart_ShouldReturnFalse()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 15, 10, 30, 0);
        var date2 = new DateTime(2024, 1, 17, 10, 30, 0);

        // Act
        var result = StaticHelpers.AreDatesEqualIgnoringTimeZone(date1, date2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RunWithRateLimit_WhenEmptyTaskList_ShouldCompleteImmediately()
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
    public async Task RunWithRateLimit_WhenSingleTask_ShouldCompleteWithoutDelay()
    {
        // Arrange
        var completed = false;
        var tasks = new List<Task>
        {
            Task.Run(() => { completed = true; })
        };

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        completed.Should().BeTrue();
        (endTime - startTime).TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task RunWithRateLimit_WhenMultipleTasksWithinBatchSize_ShouldCompleteWithoutDelay()
    {
        // Arrange
        var completedCount = 0;
        var tasks = new List<Task>
        {
            Task.Run(() => { Interlocked.Increment(ref completedCount); }),
            Task.Run(() => { Interlocked.Increment(ref completedCount); }),
            Task.Run(() => { Interlocked.Increment(ref completedCount); })
        };

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(tasks, batchSize: 5);
        var endTime = DateTime.UtcNow;

        // Assert
        completedCount.Should().Be(3);
        (endTime - startTime).TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task RunWithRateLimit_WhenTasksExceedBatchSize_ShouldRespectDelay()
    {
        // Arrange
        var completedCount = 0;
        var tasks = new List<Task>();
        for (int i = 0; i < 7; i++)
        {
            tasks.Add(Task.Run(() => { Interlocked.Increment(ref completedCount); }));
        }

        // Act
        var startTime = DateTime.UtcNow;
        await StaticHelpers.RunWithRateLimit(tasks, batchSize: 3, delayMs: 100);
        var endTime = DateTime.UtcNow;

        // Assert
        completedCount.Should().Be(7);
        (endTime - startTime).TotalMilliseconds.Should().BeGreaterOrEqualTo(200); // 2 delays of 100ms each
    }

    [Fact]
    public async Task RunWithRateLimit_WhenTasksThrowException_ShouldPropagateException()
    {
        // Arrange
        var tasks = new List<Task>
        {
            Task.Run(() => throw new InvalidOperationException("Test exception"))
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => StaticHelpers.RunWithRateLimit(tasks));
    }
}

