using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

public class InsuranceModel
{
    /// <summary>
    /// The date in which the insurance is valid from
    /// </summary>
    [JsonPropertyName("validFrom")]
    public DateTime ValidFrom { get; set; }
    /// <summary>
    /// The date in which the insurance is valid until
    /// </summary>
    [JsonPropertyName("validTo")]
    public DateTime ValidTo { get; set; }
}