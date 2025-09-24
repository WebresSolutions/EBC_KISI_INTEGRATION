using Itm.LinkSafeKisiSynchronisation.KisisModels;
using Itm.LinkSafeKisiSynchronisation.LinkSafeModels;
using Microsoft.Extensions.Options;

namespace Itm.LinkSafeKisiSynchronisation.Models;

public class MatchedModel
{
    public MatchedModel(WorkerModel worker, IOptions<KisisConfig> _config)
    {
        ArgumentNullException.ThrowIfNull(worker);

        if (worker.Contractor is null)
            throw new Exception("The worker does not contain a contractor?");

        DateTime timeStamp = DateTime.UtcNow;
        // The valid from date will always be the first induction date which is not expired
        DateTime? validFrom = worker.Inductions
            .Where(x => x.ExpiresOnUtc.Date > timeStamp.Date)
            .MinBy(x => x.InductedOnUtc)?.InductedOnUtc;

        // Find the expiry date. This will be the earlier date between the induction expiry date and the insurance expiry of the contractor
        DateTime? inductionExpiry = worker.Inductions
            .MaxBy(x => x.ExpiresOnUtc)?.ExpiresOnUtc;
        DateTime? contractorWorkerExpiry = worker.Contractor?.Records
            .MaxBy(x => x.ExpiresOnUtc)?.ExpiresOnUtc;
        EmailAddress = worker.EmailAddress;

        if (validFrom is null || inductionExpiry is null || contractorWorkerExpiry is null || worker.Contractor is null)
        {
            ValidFrom = null;
            ValidTo = null;
            IsCompliant = false;
        }
        else
        {
            ValidFrom = validFrom;
            //ValidTo = contractorWorkerExpiry < inductionExpiry ? contractorWorkerExpiry : inductionExpiry;
            ValidTo = inductionExpiry;
            WorkerModel = worker;
            KisiName = GetName(worker.EmailAddress, worker.FirstName, worker.PrimaryContractor!.DisplayName, _config, ValidFrom, ValidTo);
            IsCompliant = worker.IsCompliant && worker.Contractor.IsCompliant && ValidTo?.Date >= timeStamp.Date && ValidFrom?.Date <= timeStamp.Date;
        }
    }

    /// <summary>
    /// The worker
    /// </summary>
    public WorkerModel WorkerModel { get; } = null!;

    /// <summary>
    /// The date which this worker is valid from
    /// </summary>
    public DateTime? ValidFrom { get; }

    /// <summary>
    /// The date which this worker is valid until
    /// </summary>
    public DateTime? ValidTo { get; }

    /// <summary>
    /// The kisi name
    /// </summary>
    public string KisiName { get; } = string.Empty;

    /// <summary>
    /// The email addres of the worker for easy lookups
    /// </summary>
    public string EmailAddress { get; }
    /// <summary>
    /// Is if the worker is compliant
    /// </summary>
    public bool IsCompliant { get; }

    /// <summary>
    /// Creates a GroupLinkCreateModel for adding or updating this worker in Kisi.
    /// </summary>
    /// <param name="_config">The config</param>
    /// <returns></returns>
    public GroupLinkCreateModel CreateGroupLinkModel(IOptions<KisisConfig> _config) => new()
    {
        GroupLink = new GroupLink()
        {
            Name = KisiName,
            Email = EmailAddress,
            GroupId = _config.Value.GroupId,
            ValidFrom = ValidFrom,
            ValidUntil = ValidTo
        }
    };

    /// <summary>
    /// Gets the name to use for the kisi group link
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="firstName">The first name</param>
    /// <param name="contractorName">The contractor name</param>
    /// <param name="_config">The configuration values</param>
    /// <param name="validFrom">Valid from date</param>
    /// <param name="validUntil">Valid until date</param>
    /// <returns>A formatted string in the correct kisi format</returns>
    private static string GetName(
        string email,
        string firstName,
        string contractorName,
        IOptions<KisisConfig> _config,
        DateTime? validFrom = null,
        DateTime? validUntil = null) =>
                   $"{_config.Value.NamePrefix} {firstName} {contractorName} {email}: {validFrom?.ToString("yyyy-MM-dd")} - {validUntil?.ToString("yyyy-MM-dd")}";
}
