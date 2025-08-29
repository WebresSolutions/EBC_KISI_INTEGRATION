using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.KisisModels;

public class IssuedBy
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}

public class GroupLinksModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("phone")]
    public object Phone { get; set; }

    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    [JsonPropertyName("issued_by_id")]
    public int IssuedById { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("link_enabled")]
    public bool LinkEnabled { get; set; }

    [JsonPropertyName("quick_response_code_type")]
    public object QuickResponseCodeType { get; set; }

    [JsonPropertyName("valid_from")]
    public DateTime? ValidFrom { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTime? ValidUntil { get; set; }

    [JsonPropertyName("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("issued_by")]
    public IssuedBy IssuedBy { get; set; }
}