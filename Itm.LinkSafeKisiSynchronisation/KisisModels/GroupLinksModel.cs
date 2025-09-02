using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.KisisModels;

/// <summary>
/// Model representing the user who issued a group link in the Kisi system.
/// </summary>
public class IssuedBy
{
    /// <summary>
    /// Gets or sets the unique identifier of the user who issued the group link.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who issued the group link.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user who issued the group link.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

/// <summary>
/// Model representing an existing group link in the Kisi system.
/// </summary>
public class GroupLinksModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the group link.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the email address associated with the group link.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the phone number associated with the group link.
    /// </summary>
    [JsonPropertyName("phone")]
    public object? Phone { get; set; }

    /// <summary>
    /// Gets or sets the ID of the group where the link is created.
    /// </summary>
    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who issued the group link.
    /// </summary>
    [JsonPropertyName("issued_by_id")]
    public int? IssuedById { get; set; }

    /// <summary>
    /// Gets or sets the display name for the group link.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether the group link is currently enabled.
    /// </summary>
    [JsonPropertyName("link_enabled")]
    public bool LinkEnabled { get; set; }

    /// <summary>
    /// Gets or sets the type of quick response code for the group link.
    /// </summary>
    [JsonPropertyName("quick_response_code_type")]
    public object? QuickResponseCodeType { get; set; }

    /// <summary>
    /// Gets or sets the start date from which the group link is valid.
    /// </summary>
    [JsonPropertyName("valid_from")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Gets or sets the end date until which the group link is valid.
    /// </summary>
    [JsonPropertyName("valid_until")]
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the group link was last used.
    /// </summary>
    [JsonPropertyName("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the group link was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the group link was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the information about the user who issued the group link.
    /// </summary>
    [JsonPropertyName("issued_by")]
    public IssuedBy? IssuedBy { get; set; }
}