using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Itm.LinkSafeKisiSynchronisation;

/// <summary>
/// service for talking to Kisis
/// </summary>
public partial class Kisis
{
    private readonly RestClient _client;
    private readonly ErrorService _errorService;
    private readonly ILogger<Kisis> _logger;
    private readonly IOptions<KisisConfig> _config;

    public Kisis(ErrorService errorService, IOptions<KisisConfig> config, ILogger<Kisis> logger)
    {
        _errorService = errorService;
        _client = new RestClient("https://api.kisi.io/");
        _client.AddDefaultHeader("Authorization", $"KISI-LOGIN {config.Value.ApiToken}");
        _config = config;
        _logger = logger;
    }

    public string GetName(string email, DateTime? validFrom = null, DateTime? validUntil = null) =>
        $"{_config.Value.NamePrefix} {email}: {validFrom} - {validUntil}";
    
    /// <summary>
    /// create a new group link
    /// </summary>
    /// <param name="email"></param>
    /// <param name="validFrom"></param>
    /// <param name="validUntil"></param>
    /// <param name="cancellationToken"></param>
    public async Task MakeGroupLink(string email, DateTime? validFrom = null, DateTime? validUntil = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RestRequest("group_links", Method.Post);
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

            var content = await _client.ExecuteAsync(request, cancellationToken: cancellationToken);
            _logger.LogInformation("Id: {id}", content.Content);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to remove group link");

            _errorService.AddErrorLog($"failed to make a group link for {email} with the ranges of {validFrom} - {validUntil}");

        }
    }

    /// <summary>
    /// remove a group link
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
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
    /// syncs group links by email with the given date ranges any group links not in the given ranges will be removed
    /// </summary>
    /// <param name="email"></param>
    /// <param name="dateRanges"></param>
    public async Task SyncGroupLinks(string email, (DateTime validFrom, DateTime validUntil)[] dateRanges,
        CancellationToken cancellationToken = default)
    {
        // email = "kaleb.mcghie@itmagnet.com.au";
        
        _logger.LogInformation("Syncing Email {Email}", email);
        int pageCount = 0;
        var pagesLeft = true;
        do
        {
            
            _logger.LogInformation("getting page {pageCount}", pageCount);
            var link = new RestRequest("group_links");
            link.AddQueryParameter("limit", 250).AddQueryParameter("offset",pageCount * 250);
            var request = await _client.GetAsync(link, cancellationToken: cancellationToken);
            var content = JsonSerializer.Deserialize<List<GroupLinksModel>>(request.Content);
            _logger.LogInformation("doing clean up");
            if (content?.Count > 0)
            {
                // filter by prefix and by email
                var filteredContent = content
                    .Where(i => i.Name?.StartsWith(_config.Value.NamePrefix) ?? false)
                    .Where(i => i.Name?.Contains(email) ?? false);
                var groupLinksModels = filteredContent.ToList();
                if (groupLinksModels.Count() != 0)
                {
                    // remove old
                    foreach (var item in groupLinksModels)
                    {
                        var found = false;
                        foreach (var range in dateRanges)
                        {
            
                            try
                            {
                                if (item.Name == GetName(email, range.validFrom, range.validUntil))
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
                            _logger.LogInformation("Removing link {linkId}", item.Id);
                            try
                            {
                                await RemoveGroupLink(item.Id, cancellationToken);
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
            content = JsonSerializer.Deserialize<List<GroupLinksModel>>(request.Content);
            _logger.LogInformation("adding new links");
            if (content?.Count > 0)
            {
                // add new
                foreach (var range in dateRanges)
                {
                    var found = false;
                    GroupLinksModel foundItem = null;
                    foreach (var item in content.Where(i => i.Name?.StartsWith(_config.Value.NamePrefix) ?? false))
                    {
                        var name = GetName(email, range.validFrom, range.validUntil);
                        if (item.Name == name)
                        {
                            found = true;
                            foundItem = item;
                        }
                    }
                    if (!found)
                    {
                        _logger.LogInformation("Making link {email}: {validFrom} - {validUntil}", email, range.validFrom, range.validUntil);
                        await MakeGroupLink(email, range.validFrom, range.validUntil, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("found group link {linkId}: {name}", foundItem.Id, foundItem.Name);
                    }

                }


            }
            _logger.LogInformation("DONE adding new links");
            var header = request.Headers.First(i =>String.Equals(i.Name, "x-collection-range", StringComparison.OrdinalIgnoreCase));
            var regex = MyRegex();
            var match = regex.Match(header.Value.ToString());
            var end = int.Parse(match.Groups["end"].Value);
            var total = int.Parse(match.Groups["total"].Value);
            if (end > total)
            {
             pagesLeft = false;
            }
            pageCount++;

        } while (pagesLeft);

    }

    [GeneratedRegex("^(?<start>\\d+)-(?<end>\\d+)/(?<total>\\d+)$")]
    private static partial Regex MyRegex();
}

public class KisisConfig
{
    public string ApiToken { get; set; }
    public int GroupId { get; set; }
    public string NamePrefix { get; set; }
}