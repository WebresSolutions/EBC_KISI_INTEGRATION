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
        try
        {

            _logger.LogInformation("C# HTTP trigger function processed a request.");
            await Process();
            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync("Welcome to Azure Functions!");

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
            // Dictionary to store worker contracts by email address
            // Key: Worker's email address
            // Value: HashSet of tuples containing (validFrom, validUntil) dates for each contract/induction period
            Dictionary<string, HashSet<(DateTime validFrom, DateTime validUntil)>> contracts = [];

            // Retrieve all workers from LinkSafe system
            WorkerModel[] workers = await linkSafe.GetWorkers();

            // Process each worker to extract their contract/induction periods
            foreach (WorkerModel worker in workers)
            {
                // Check if we already have an entry for this worker's email address
                // If not, create a new HashSet to store their contract periods
                if (!contracts.TryGetValue(worker.EmailAddress, out HashSet<(DateTime validFrom, DateTime validUntil)>? value))
                {
                    value = [];
                    contracts.Add(worker.EmailAddress, value);
                }

                // Add each induction period (contract period) for this worker
                // Each induction represents a valid access period with start and end dates
                foreach (InductionModel induction in worker.Inductions)
                    value.Add((induction.InductedOnUtc, induction.ExpiresOnUtc));
            }

            // Synchronize each worker's access permissions in Kisi system
            // For each worker, send their contract periods to Kisi to update their access rights
            foreach (KeyValuePair<string, HashSet<(DateTime validFrom, DateTime validUntil)>> contract in contracts)
                await kisis.SyncGroupLinks(contract.Key, [.. contract.Value]);
        }
        catch (Exception ex)
        {
            // Log any unhandled errors that occur during the synchronization process
            errorService.AddErrorLog($"an unhandeled error happened {ex}");
        }
        finally
        {
            // Always send error logs, regardless of success or failure
            await errorService.Send();
        }
    }
}