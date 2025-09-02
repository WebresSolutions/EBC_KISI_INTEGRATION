using Itm.LinkSafeKisiSynchronisation.KisisModels;
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
    /// Synchronizes group links for a specific email address by ensuring only valid date ranges exist.
    /// Removes any existing group links that don't match the provided date ranges and creates new ones as needed.
    /// </summary>
    /// <param name="email">The email address to synchronize group links for</param>
    /// <param name="dateRanges">Array of valid date ranges for the worker's access</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task SyncGroupLinks(string email, (DateTime validFrom, DateTime validUntil)[] dateRanges,
        CancellationToken cancellationToken = default)
    {
        // email = "kaleb.mcghie@itmagnet.com.au";

        _logger.LogInformation("Syncing Email {Email}", email);
        int pageCount = 0;
        bool pagesLeft = true;
        do
        {

            _logger.LogInformation("getting page {pageCount}", pageCount);
            (RestRequest link, RestResponse request, List<GroupLinksModel> content) = await GetGroupLinks(pageCount, cancellationToken);

            List<Tuple<string?, int>> links = content.GroupBy(x => x.Name).Select(a => Tuple.Create(a.Key, a.Count())).ToList();

            _logger.LogInformation("doing clean up");
            if (content?.Count > 0)
            {
                // filter by prefix and by email
                IEnumerable<GroupLinksModel> filteredContent = content
                    .Where(i => i.Name?.StartsWith(_config.Value.NamePrefix) ?? false)
                    .Where(i => i.Name?.Contains(email) ?? false);
                List<GroupLinksModel> groupLinksModels = [.. filteredContent];
                if (groupLinksModels.Count is not 0)
                {
                    // remove old
                    foreach (GroupLinksModel? groupLink in groupLinksModels)
                    {
                        bool found = false;
                        foreach ((DateTime validFrom, DateTime validUntil) range in dateRanges)
                        {

                            try
                            {
                                if (groupLink.Name == GetName(email, range.validFrom, range.validUntil))
                                {
                                    found = true;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogCritical(e, "");
                            }

                        }

                        if (!found)
                        {
                            _logger.LogInformation("Removing link {linkId}", groupLink.Id);
                            try
                            {
                                await RemoveGroupLink(groupLink.Id, cancellationToken);
                            }
                            catch (Exception e)
                            {
                                _logger.LogCritical(e, "HOW?");
                            }
                        }
                    }

                }
            }
            _logger.LogInformation("DONE doing clean up");


            request = await _client.GetAsync(link, cancellationToken: cancellationToken);
            content = JsonSerializer.Deserialize<List<GroupLinksModel>>(request.Content) ?? [];
            _logger.LogInformation("adding new links");
            if (content?.Count > 0)
            {
                // add new
                foreach ((DateTime validFrom, DateTime validUntil) in dateRanges)
                {
                    bool found = false;
                    GroupLinksModel? foundItem = null;
                    foreach (GroupLinksModel? item in content.Where(i => i.Name?.StartsWith(_config.Value.NamePrefix) ?? false))
                    {
                        string name = GetName(email, validFrom, validUntil);
                        if (item.Name == name)
                        {
                            found = true;
                            foundItem = item;
                        }
                    }
                    if (!found)
                    {
                        _logger.LogInformation("Making link {email}: {validFrom} - {validUntil}", email, validFrom, validUntil);
                        await MakeGroupLink(email, validFrom, validUntil, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("found group link {linkId}: {name}", foundItem?.Id, foundItem?.Name);
                    }

                }


            }
            _logger.LogInformation("DONE adding new links");
            HeaderParameter header = request.Headers.First(i => String.Equals(i.Name, "x-collection-range", StringComparison.OrdinalIgnoreCase));
            Regex regex = MyRegex();
            Match match = regex.Match(header.Value.ToString());
            int end = int.Parse(match.Groups["end"].Value);
            int total = int.Parse(match.Groups["total"].Value);
            if (end > total)
            {
                pagesLeft = false;
            }
            pageCount++;

        } while (pagesLeft);

        async Task<(RestRequest link, RestResponse request, List<GroupLinksModel> content)> GetGroupLinks(int pageCount, CancellationToken cancellationToken)
        {
            RestRequest link = new("group_links");
            link.AddQueryParameter("limit", 250).AddQueryParameter("offset", pageCount * 250);
            RestResponse request = await _client.GetAsync(link, cancellationToken: cancellationToken);

            if (request.Content is null)
                return (link, request, []);

            List<GroupLinksModel>? content = JsonSerializer.Deserialize<List<GroupLinksModel>>(request.Content);
            return (link, request, content);
        }
    }

    /// <summary>
    /// Generates a standardized name for group links based on email and validity dates.
    /// </summary>
    /// <param name="email">The email address of the worker</param>
    /// <param name="validFrom">Optional start date for validity period</param>
    /// <param name="validUntil">Optional end date for validity period</param>
    /// <returns>A formatted name string for the group link</returns>
    public string GetName(string email, DateTime? validFrom = null, DateTime? validUntil = null) =>
        $"{_config.Value.NamePrefix} {email}: {validFrom} - {validUntil}";

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
    /// Removes a group link from Kisi by its ID.
    /// </summary>
    /// <param name="id">The ID of the group link to remove</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RemoveGroupLink(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteAsync(new RestRequest($"group_link/{id}"), cancellationToken: cancellationToken);
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