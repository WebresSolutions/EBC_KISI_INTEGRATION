namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

/// <summary>
/// Model representing a collection of contractors returned from the LinkSafe API.
/// </summary>
public class ContractorsModel
{
    /// <summary>
    /// Gets or sets the array of contractor models.
    /// </summary>
    public ContractorModel[] Contractors { get; set; } = [];
}

/// <summary>
/// Model representing a contractor with their contact information from LinkSafe.
/// </summary>
public class ContractorModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the contractor in LinkSafe.
    /// </summary>
    public int ContractorId { get; set; }

    /// <summary>
    /// Gets or sets the array of contact records for the contractor.
    /// </summary>
    public ContactModel[] Contacts { get; set; } = [];
}

/// <summary>
/// Model representing a contact record for a contractor.
/// </summary>
public class ContactModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the contact record.
    /// </summary>
    public int ContactId { get; set; }
    
    /// <summary>
    /// Gets or sets the type of contact (e.g., primary, secondary).
    /// </summary>
    public string ContactType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the contact.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;
}