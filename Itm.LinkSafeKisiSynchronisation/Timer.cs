using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// Azure Function that synchronizes worker access permissions between LinkSafe and Kisi systems.
/// Runs on a timer trigger and can also be invoked via HTTP.
/// </summary>
/// <remarks>
/// Initializes a new instance of the Timer class with required dependencies.
/// </remarks>
/// <param name="loggerFactory">Factory for creating loggers</param>
/// <param name="errorService">Service for handling and reporting errors</param>
/// <param name="linkSafe">Service for interacting with LinkSafe API</param>
/// <param name="kisis">Service for interacting with Kisi API</param>
public class Timer(
    ILoggerFactory loggerFactory,
    ErrorService errorService,
    LinkSafe linkSafe,
    Kisis kisis
        )
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Timer>();

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
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        await Process();
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        await response.WriteStringAsync("Welcome to Azure Functions!");

        return response;

    }

    /// <summary>
    /// Timer trigger function that runs the synchronization process daily at midnight.
    /// </summary>
    /// <param name="myTimer">Timer information from the Azure Functions runtime</param>
    /// <returns>A task representing the asynchronous operation</returns>
    [Function("Timer")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        await Process();
    }


    /// <summary>
    /// Main synchronization process that retrieves workers from LinkSafe and syncs their access permissions in Kisi.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task Process()
    {
        try
        {
            Dictionary<string, HashSet<(DateTime validFrom, DateTime validUntil)>> contracts = [];

            WorkerModel[] workers = await linkSafe.GetWorkers();
            foreach (WorkerModel worker in workers)
            {
                if (!contracts.TryGetValue(worker.EmailAddress, out HashSet<(DateTime validFrom, DateTime validUntil)>? value))
                {
                    value = [];
                    contracts.Add(worker.EmailAddress, value);
                }
                foreach (InductionModel induction in worker.Inductions)
                    value.Add((induction.InductedOnUtc, induction.ExpiresOnUtc));
            }

            foreach (KeyValuePair<string, HashSet<(DateTime validFrom, DateTime validUntil)>> contract in contracts)
                await kisis.SyncGroupLinks(contract.Key, [.. contract.Value]);
        }
        catch (Exception ex)
        {
            errorService.AddErrorLog($"an unhandeled error happened {ex}");
        }
        finally
        {
            await errorService.Send();
        }
    }
}