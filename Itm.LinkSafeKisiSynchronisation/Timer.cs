using System;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Itm.LinkSafeKisiSynchronisation;

public class Timer
{
    private readonly ILogger _logger;
    private readonly ErrorService _errorService;
    private readonly LinkSafe _linkSafe;
    private readonly Kisis _kisis;

    public Timer(
        ILoggerFactory loggerFactory,
        ErrorService errorService,
        LinkSafe linkSafe,
        Kisis kisis
        )
    {
        _logger = loggerFactory.CreateLogger<Timer>();
        _errorService = errorService;
        _linkSafe = linkSafe;
        _kisis = kisis;
    }

    public async Task Process()
    {
        try
        {
            var contracts =
                new Dictionary<string, HashSet<(DateTime validFrom, DateTime validUntil)>>();

             foreach (var item in await _linkSafe.GetWorkers())
            {
                if (!contracts.ContainsKey(item.EmailAddress))
                {
                    contracts.Add(item.EmailAddress, new HashSet<(DateTime validFrom, DateTime validUntil)>());
                }

                foreach (var induction in item.Inductions)
                {
                    contracts[item.EmailAddress].Add((induction.InductedOnUtc, induction.ExpiresOnUtc));
                }
            }

            foreach (var item in contracts)
            {
                await _kisis.SyncGroupLinks(item.Key, item.Value.ToArray());
            }

        }
        catch (Exception ex)
        {
            _errorService.AddErrorLog($"an unhandeled error happened {ex}");
        }
        finally
        {
            await _errorService.Send();
        }
    }

    [Function("HttpTrigger")]
    public async Task<HttpResponseData> RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        await Process();
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        await response.WriteStringAsync("Welcome to Azure Functions!");

        return response;
        
    }
    
    [Function("Timer")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        await Process();
    }
}