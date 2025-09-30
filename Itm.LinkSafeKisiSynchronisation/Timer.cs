using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// Azure Function that synchronizes worker access permissions between LinkSafe and Kisi systems.
/// Runs on a timer trigger and can also be invoked via HTTP.
/// </summary>
/// <remarks>
/// Initializes a new instance of the Timer class with required dependencies.
/// </remarks>
/// <param name="_logger">Factory for creating loggers</param>
/// <param name="_errorService">Service for handling and reporting errors</param>
/// <param name="_linkSafeService">Service for interacting with LinkSafe API</param>
/// <param name="_kisis">Service for interacting with Kisi API</param>
public class Timer(
    ILoggerFactory _logger,
    ErrorService _errorService,
    LinkSafe _linkSafeService,
    Kisis _kisis,
    IOptions<KisisConfig> _config
        )
{
    private readonly ILogger _logger = _logger.CreateLogger<Timer>();

    /// <summary>
    /// HTTP trigger function that allows manual execution of the synchronization process.
    /// </summary>
    /// <param name="req">The HTTP request data</param>
    /// <param name="executionContext">The function execution context</param>
    /// <returns>HTTP response indicating success</returns>
    [Function("HttpTrigger")]
    public async Task<HttpResponseData> RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            (int added, int updated, int deleted) = await SynchonizeKisiAndLinksafe();

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync($"Successfully completed the synchronization of linksafe and kisi! Removed: {deleted} workers. Added {added} workers. Updated {updated} workers.");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            HttpResponseData response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync($"An Error Occurred while Executing the function");
            return response;
        }
    }

    /// <summary>
    /// Timer trigger function that runs the synchronization process daily at midnight.
    /// </summary>
    /// <param name="myTimer">Timer information from the Azure Functions runtime</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [Function("Timer")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo) => await SynchonizeKisiAndLinksafe();

    /// <summary>
    /// Synchonizes workers between LinkSafe and Kisi systems.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(int added, int updated, int deleted)> SynchonizeKisiAndLinksafe(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create the mathced models from the linksafe workers
            MatchedModel[] allWorkers = [.. (await _linkSafeService.MatchWorkersToTheirContractor()).Select(x => new MatchedModel(x, _config))];

            // Get all of the kisi workers
            List<GroupLinksModel> kisiModels = await _kisis.GetGroupLinks(0, [], cancellationToken);

            // Find the list of workers to remove from kisi 
            List<MatchedModel> nonCompliantWorkers = [.. allWorkers.Where(x => !x.IsCompliant)];

            // Find the list of workers to add to kisi
            List<MatchedModel> compliantWorkers = [.. allWorkers.Where(x => x.IsCompliant)];

            // Determin which workers will need to be updated and created
            Dictionary<GroupLinksModel, MatchedModel> workersToUpdate = [];
            List<MatchedModel> workersToAdd = [];

            foreach (MatchedModel workerToCheck in compliantWorkers)
            {
                GroupLinksModel? kisiModel = kisiModels.FirstOrDefault(x => x.Email.Equals(workerToCheck.EmailAddress, StringComparison.OrdinalIgnoreCase));
                if (kisiModel is not null)
                {
                    if (GrooupLinkNeedsToBeUpdated(workerToCheck, kisiModel))
                        workersToUpdate.Add(kisiModel, workerToCheck);
                }
                else
                    workersToAdd.Add(workerToCheck);
            }

            // Remove the non compliant workers from kisi.
            GroupLinksModel[] groupIdsToRemove = [.. kisiModels
            .Where(x => nonCompliantWorkers
                .Any(y => x.Email.Equals(y.EmailAddress, StringComparison.OrdinalIgnoreCase)))];

            // Remove the non compliant workers from kisi. Create an ienumberable of tasks to remove the group links
            await UpdateAndRemoveInKisi(workersToUpdate, workersToAdd, groupIdsToRemove, cancellationToken);

            kisiModels = [.. kisiModels.Except(groupIdsToRemove)];

            // Add the compliant workers
            return (workersToAdd.Count, workersToUpdate.Count, groupIdsToRemove.Length);
        }
        catch (Exception ex)
        {
            _errorService.AddErrorLog(ex.Message);
            _logger.LogError(ex, "An error occurred during synchronization.");
            throw;
        }
    }

    /// <summary>
    /// Updates and removes group links in Kisi based on the provided lists of workers to update, add, and remove.
    /// </summary>
    /// <param name="workersToUpdate">The workers to update</param>
    /// <param name="workersToAdd">The workers to add</param>
    /// <param name="groupIdsToRemove">The workers to remove</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    private async Task UpdateAndRemoveInKisi(Dictionary<GroupLinksModel, MatchedModel> workersToUpdate, List<MatchedModel> workersToAdd, GroupLinksModel[] groupIdsToRemove, CancellationToken cancellationToken)
    {
        foreach (GroupLinksModel group in groupIdsToRemove)
            await _kisis.RemoveGroupLink(group.Id, cancellationToken);

        foreach (MatchedModel model in workersToAdd)
            await _kisis.MakeGroupLink(model, cancellationToken);

        // In this scenario updates are just removes and adds
        foreach (KeyValuePair<GroupLinksModel, MatchedModel> item in workersToUpdate)
            await _kisis.RemoveGroupLink(item.Key.Id, cancellationToken);

        foreach (KeyValuePair<GroupLinksModel, MatchedModel> item in workersToUpdate)
            await _kisis.MakeGroupLink(item.Value, cancellationToken);
    }

    /// <summary>
    /// Determines if a group link needs to be updated based on differences between the matched model and the existing Kisi model.
    /// </summary>
    /// <param name="matchedModel">The matched model</param>
    /// <param name="kisiModel">The Kisi Model</param>
    /// <returns>false if matching</returns>
    private static bool GrooupLinkNeedsToBeUpdated(MatchedModel matchedModel, GroupLinksModel kisiModel)
    {
        string groupSafeName = matchedModel.KisiName.Split(':')[0];
        string kisiName = kisiModel.Name!.Split(':')[0];

        if (!groupSafeName.Equals(kisiName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if the valid from dates are different (ignore time zones)
        if (!StaticHelpers.AreDatesEqualIgnoringTimeZone(kisiModel.ValidFrom, matchedModel.ValidFrom))
            return true;
        // Check if the valid to dates are different (ignore time zones)
        return !StaticHelpers.AreDatesEqualIgnoringTimeZone(kisiModel.ValidUntil, matchedModel.ValidTo);
    }
}