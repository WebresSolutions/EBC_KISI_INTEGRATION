using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

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
        WorkersModel? content = await _client.GetJsonAsync<WorkersModel>("2.0/Compliance/Workers/List");
        return content?.Workers ?? [];
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
    public string ApiToken { get; set; }
}