
using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

/// <summary>
/// Model representing a collection of contractors returned from the LinkSafe API.
/// </summary>
public class ContractorsModel
{
    /// <summary>
    /// Gets or sets the array of contractor models.
    /// </summary>
    [JsonPropertyName("contractors")]
    public Contractor[] Contractors { get; set; } = [];
}

public class Contractor
{
    [JsonPropertyName("contractorID")]
    public int ContractorID { get; set; }

    [JsonPropertyName("externalReferenceNumber")]
    public object? ExternalReferenceNumber { get; set; }

    [JsonPropertyName("principalContractorID")]
    public object? PrincipalContractorID { get; set; }

    [JsonPropertyName("isCompliant")]
    public bool IsCompliant { get; set; }

    [JsonPropertyName("nearExpiringItems")]
    public int NearExpiringItems { get; set; }

    [JsonPropertyName("nonCompliantItems")]
    public int NonCompliantItems { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("tradingName")]
    public object? TradingName { get; set; }

    [JsonPropertyName("stateCode")]
    public string StateCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; set; } =string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("contacts")]
    public List<Contact> Contacts { get; set; } = [];

    [JsonPropertyName("complianceItems")]
    public List<ComplianceItem> ComplianceItems { get; set; } = [];

    [JsonPropertyName("records")]
    public List<Record> Records { get; set; } = [];
}

public class Contact
{
    [JsonPropertyName("contactID")]
    public int ContactID { get; set; }

    [JsonPropertyName("contactType")]
    public string ContactType { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
}

public class ComplianceItem
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("itemDescription")]
    public string ItemDescription { get; set; } = string.Empty;

    [JsonPropertyName("complianceItemID")]
    public int? ComplianceItemID { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("isCompliant")]
    public bool IsCompliant { get; set; }

    [JsonPropertyName("isNearExpiring")]
    public bool IsNearExpiring { get; set; }
}

public class Record
{
    [JsonPropertyName("recordID")]
    public int RecordID { get; set; }

    [JsonPropertyName("recordType")]
    public string RecordType { get; set; } = string.Empty;

    [JsonPropertyName("recordTypeID")]
    public int RecordTypeID { get; set; }

    [JsonPropertyName("expiryType")]
    public string ExpiryType { get; set; } = string.Empty;

    [JsonPropertyName("specialInstructions")]
    public string SpecialInstructions { get; set; } = string.Empty;

    [JsonPropertyName("templateFileName")]
    public string TemplateFileName { get; set; } = string.Empty;

    [JsonPropertyName("templateFileSize")]
    public int? TemplateFileSize { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("expiresOnUtc")]
    public DateTime ExpiresOnUtc { get; set; }

    [JsonPropertyName("recordStatus")]
    public string RecordStatus { get; set; } = string.Empty;

    [JsonPropertyName("createdOnUtc")]
    public DateTime CreatedOnUtc { get; set; }
}