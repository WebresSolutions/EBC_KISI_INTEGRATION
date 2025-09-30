using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.KisisModels;

/// <summary>
/// Model representing a group link in the Kisi system for access control.
/// </summary>
public class GroupLink
{
    /// <summary>
    /// Gets or sets the email address associated with the group link.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the ID of the group where the link will be created.
    /// </summary>
    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the display name for the group link.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the phone number associated with the group link.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; }

    /// <summary>
    /// Gets or sets the type of quick response code for the group link.
    /// </summary>
    [JsonPropertyName("quick_response_code_type")]
    public string QuickResponseCodeType { get; set; }

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
}

/// <summary>
/// Model for creating a new group link in the Kisi system.
/// </summary>
public class GroupLinkCreateModel
{
    /// <summary>
    /// Gets or sets the group link data to be created.
    /// </summary>
    [JsonPropertyName("group_link")]
    public GroupLink GroupLink { get; set; }
}
