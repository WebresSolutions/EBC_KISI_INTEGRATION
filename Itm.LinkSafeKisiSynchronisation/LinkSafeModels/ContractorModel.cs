namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

public class ContractorsModel
{
    public ContractorModel[] Contractors { get; set; }
}

public class ContractorModel
{
    public int ContractorId { get; set; }
    public ContactModel[] Contacts { get; set; }
}

public class ContactModel
{
    public int ContactId { get; set; }
    public string ContactType { get; set; }
    public string EmailAddress { get; set; }
}