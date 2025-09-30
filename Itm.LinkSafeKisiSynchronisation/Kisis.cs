using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// Service for interacting with the Kisi API to manage group links and access permissions.
/// Handles creation, removal, and synchronization of group links based on worker induction validity periods.
/// </summary>
public partial class Kisis
{
    private readonly RestClient _client;
    private readonly ErrorService _errorService;
    private readonly ILogger<Kisis> _logger;
    private readonly IOptions<KisisConfig> _config;

    /// <summary>
    /// Initializes a new instance of the Kisi service with required dependencies.
    /// </summary>
    /// <param name="errorService">Service for handling and reporting errors</param>
    /// <param name="config">Configuration options for Kisi API</param>
    /// <param name="logger">Logger for recording operations</param>
    public Kisis(ErrorService errorService, IOptions<KisisConfig> config, ILogger<Kisis> logger)
    {
        _errorService = errorService;
        _client = new RestClient("https://api.kisi.io/");
        _client.AddDefaultHeader("Authorization", $"KISI-LOGIN {config.Value.ApiToken}");
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Recursively get all of the data from the kisi API 
    /// </summary>
    /// <param name="pageOffset">The initialoffset</param>
    /// <param name="list">The list set the empty initially </param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<GroupLinksModel>> GetGroupLinks(int pageOffset, List<GroupLinksModel> list, CancellationToken cancellationToken)
    {
        RestRequest link = new("group_links");
        link.AddQueryParameter("limit", 250).AddQueryParameter("offset", pageOffset * 250);
        RestResponse request = await _client.GetAsync(link, cancellationToken: cancellationToken);

        if (request.Content is null)
            return list;

        List<GroupLinksModel> content = JsonSerializer.Deserialize<List<GroupLinksModel>>(request.Content) ?? throw new Exception("failed to serialize the kisis response");
        list.AddRange(content);

        // Read the headers the ensure there are no more pages
        HeaderParameter header = request.Headers!.First(i => String.Equals(i.Name, "x-collection-range", StringComparison.OrdinalIgnoreCase));
        Regex regex = MyRegex();
        Match match = regex.Match(header.Value.ToString());

        int end = int.Parse(match.Groups["end"].Value);
        int total = int.Parse(match.Groups["total"].Value);

        // Recurrsively get more
        if (total > end)
        {
            // Safety to stop infinite loop bc there shouldnt be more than 2500 records
            if (pageOffset > 10)
                return list;

            pageOffset++;
            await GetGroupLinks(pageOffset, list, cancellationToken);
        }

        return list;
    }

    /// <summary>
    /// Generates a standardized name for group links based on email and validity dates.
    /// </summary>
    /// <param name="email">The email address of the worker</param>
    /// <param name="validFrom">Optional start date for validity period</param>
    /// <param name="validUntil">Optional end date for validity period</param>
    /// <returns>A formatted name string for the group link</returns>
    public string GetName(string email, DateTime? validFrom = null, DateTime? validUntil = null)
    {
        return $"{_config.Value.NamePrefix} {email}: {validFrom} - {validUntil}";
    }

    /// <summary>
    /// Creates a new group link in Kisi for a worker with specified validity dates.
    /// </summary>
    /// <param name="email">The email address of the worker</param>
    /// <param name="validFrom">Optional start date for access validity</param>
    /// <param name="validUntil">Optional end date for access validity</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task MakeGroupLink(string email, DateTime? validFrom = null, DateTime? validUntil = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            RestRequest request = new("group_links", Method.Post);
            request.AddJsonBody(new GroupLinkCreateModel
            {
                GroupLink = new GroupLink()
                {
                    Name = GetName(email, validFrom, validUntil),
                    Email = email,
                    GroupId = _config.Value.GroupId,
                    ValidUntil = validUntil,
                    ValidFrom = validFrom

                }

            });

            RestResponse content = await _client.ExecuteAsync(request, cancellationToken: cancellationToken);
            _logger.LogInformation("Id: {id}", content.Content);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to remove group link");

            _errorService.AddErrorLog($"failed to make a group link for {email} with the ranges of {validFrom} - {validUntil}");
        }
    }

    /// <summary>
    /// Creates a new group link in Kisi for a worker based on a MatchedModel containing email and validity dates.
    /// </summary>
    /// <param name="model">The model being used to create the group link</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task MakeGroupLink(MatchedModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            RestRequest request = new("group_links", Method.Post);
            request.AddJsonBody(model.CreateGroupLinkModel(_config));
            Console.WriteLine($"Added Worker: {model.WorkerModel.FirstName}");

            RestResponse content = await _client.ExecuteAsync(request, cancellationToken: cancellationToken);
            _logger.LogInformation("Id: {id}", content.Content);

        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to remove group link");
            _errorService.AddErrorLog($"failed to make a group link for {model.EmailAddress} with the ranges of {model.ValidFrom} - {model.ValidTo}");
        }
    }

    /// <summary>
    /// Removes a group link from Kisi by its ID.
    /// </summary>
    /// <param name="id">The ID of the group link to remove</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RemoveGroupLink(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"Removed Worker: {id}");
            RestRequest restRequest = new($"group_links/{id}", Method.Delete);
            RestResponse result = await _client.ExecuteAsync(restRequest, cancellationToken: cancellationToken);
            ;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to remove group link");
            _errorService.AddErrorLog($"failed to remove the group link Id:{id}");
        }
    }


    /// <summary>
    /// Generates a regex pattern for parsing collection range headers from Kisi API responses.
    /// </summary>
    /// <returns>A compiled regex pattern for parsing range headers</returns>
    [GeneratedRegex("^(?<start>\\d+)-(?<end>\\d+)/(?<total>\\d+)$")]
    private static partial Regex MyRegex();
}

/// <summary>
/// Configuration options for the Kisi API integration.
/// </summary>
public class KisisConfig
{
    /// <summary>
    /// Gets or sets the API token for authenticating with Kisi.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group ID where workers will be added for access permissions.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the prefix used for naming group links in Kisi.
    /// </summary>
    public string NamePrefix { get; set; } = string.Empty;
}