using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.KisisModels;

public class GroupLink
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; }

    [JsonPropertyName("quick_response_code_type")]
    public string QuickResponseCodeType { get; set; }

    [JsonPropertyName("valid_from")]
    public DateTime? ValidFrom { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTime? ValidUntil { get; set; }
}

public class GroupLinkCreateModel
{
    [JsonPropertyName("group_link")]
    public GroupLink GroupLink { get; set; }
}
