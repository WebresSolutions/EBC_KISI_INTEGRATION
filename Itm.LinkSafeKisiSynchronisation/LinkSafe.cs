using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Itm.LinkSafeKisiSynchronisation;

public class LinkSafe
{
    private readonly ErrorService _errorService;
    private readonly RestClient _client;
    private readonly IOptions<LinkSafeConfig> _config;
    private readonly ILogger<LinkSafe> _logger;

    public LinkSafe(ErrorService errorService, IOptions<LinkSafeConfig> config, ILogger<LinkSafe> logger)
    {
        _errorService = errorService;
        _client = new RestClient("https://api.linksafe.com.au/");
        _client.AddDefaultHeader("apikey", $"{config.Value.ApiToken}");
        _config = config;
        _logger = logger;
    }

    public async Task<WorkerModel[]> GetWorkers()
    {
        var content = await _client.GetJsonAsync<WorkersModel>("2.0/Compliance/Workers/List");
        return content.Workers;

    }

}

public class LinkSafeConfig
{
    public string ApiToken { get; set; }
}