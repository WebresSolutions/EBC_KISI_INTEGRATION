
namespace Itm.LinkSafeKisiSynchronisation;

internal static class StaticHelpers
{
    /// <summary>
    /// Are dates equal ignoring time zone differences
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool AreDatesEqualIgnoringTimeZone(DateTime? a, DateTime? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;

        // Compare only the date part and allow a difference of 1 day to account for time zone differences
        var daysDifference = Math.Abs((a.Value.Date - b.Value.Date).TotalDays);
        return daysDifference <= 1;
    }

    /// <summary>
    /// Runs tasks in batches with a specified delay between batches to respect rate limits.
    /// </summary>
    /// <param name="tasks"></param>
    /// <param name="batchSize"></param>
    /// <param name="delayMs"></param>
    /// <returns></returns>
    public static async Task RunWithRateLimit(IEnumerable<Task> tasks, int batchSize = 5, int delayMs = 1000)
    {
        var taskList = tasks.ToList();
        for (int i = 0; i < taskList.Count; i += batchSize)
        {
            var batch = taskList.Skip(i).Take(batchSize).ToList();
            await Task.WhenAll(batch);
            if (i + batchSize < taskList.Count)
                await Task.Delay(delayMs);
        }
    }
}
