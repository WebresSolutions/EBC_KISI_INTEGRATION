using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Text.Json;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// Service for interacting with the LinkSafe API to retrieve worker and induction information.
/// </summary>
public class LinkSafe
{
    private readonly ErrorService _errorService;
    private readonly RestClient _client;
    private readonly IOptions<LinkSafeConfig> _config;
    private readonly ILogger<LinkSafe> _logger;

    /// <summary>
    /// Initializes a new instance of the LinkSafe service with required dependencies.
    /// </summary>
    /// <param name="errorService">Service for handling and reporting errors</param>
    /// <param name="config">Configuration options for LinkSafe API</param>
    /// <param name="logger">Logger for recording operations</param>
    public LinkSafe(ErrorService errorService, IOptions<LinkSafeConfig> config, ILogger<LinkSafe> logger)
    {
        _errorService = errorService;
        _client = new RestClient("https://api.linksafe.com.au/");
        _client.AddDefaultHeader("apikey", $"{config.Value.ApiToken}");
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all workers and their induction records from the LinkSafe API.
    /// </summary>
    /// <returns>An array of worker models containing induction information</returns>
    public async Task<WorkerModel[]> GetWorkers()
    {
        var response = await _client.ExecuteGetAsync("2.0/Compliance/Workers/List");

        if (response.Content == null)
            return [];

        WorkersModel? content = JsonSerializer.Deserialize<WorkersModel>(response.Content);
        if (content is null)
            return [];

#if DEBUG
        if (content.Workers is [] or null)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json/workers.json");
            string workersString = await File.ReadAllTextAsync(path);
            content = JsonSerializer.Deserialize<WorkersModel>(workersString);
        }
#endif

        return content?.Workers ?? [];
    }

    public async Task<Contractor[]> GetContractors()
    {
        var response = await _client.ExecuteGetAsync("2.0/Compliance/Contractors/List");

        if (response.Content == null)
            return [];

        ContractorsModel? content = JsonSerializer.Deserialize<ContractorsModel>(response.Content);
        if (content is null)
            return [];

#if DEBUG
        if (content.Contractors is [] or null)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json/contractors.json");
            string contractorString = await File.ReadAllTextAsync(path);
            content = JsonSerializer.Deserialize<ContractorsModel>(contractorString);
        }
#endif

        return content?.Contractors ?? [];
    }

    /// <summary>
    /// Matches the worker to its related contractor
    /// </summary>
    /// <returns></returns>
    public async Task<WorkerModel[]> MatchWorkersToTheirContractor()
    {
        WorkerModel[] workers = await GetWorkers();
        Contractor[] contractors = await GetContractors();

        foreach (WorkerModel worker in workers)
        {
            // Find the related contractor
            Contractor? contractor = contractors.FirstOrDefault(x => x.ContractorID == worker.PrimaryContractor?.ContactorID);
            if (contractor is not null)
                worker.Contractor = contractor; 
        }

        return workers;
    }
}

/// <summary>
/// Configuration options for the LinkSafe API integration.
/// </summary>
public class LinkSafeConfig
{
    /// <summary>
    /// Gets or sets the API token for authenticating with LinkSafe.
    /// </summary>
    public required string ApiToken { get; set; }
}