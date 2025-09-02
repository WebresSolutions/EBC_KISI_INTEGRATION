using System.Text.Json.Serialization;

namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

/// <summary>
/// Model representing a collection of workers returned from the LinkSafe API.
/// </summary>
public class WorkersModel
{
    /// <summary>
    /// Gets or sets the array of worker models.
    /// </summary>
    [JsonPropertyName("workers")]
    public WorkerModel[] Workers { get; set; } = [];
}

/// <summary>
/// Model representing a worker with their induction records from LinkSafe.
/// </summary>
public class WorkerModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the worker in LinkSafe.
    /// </summary>
    [JsonPropertyName("workerID")]
    public int WorkerId { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the worker.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the array of induction records for the worker.
    /// </summary>
    [JsonPropertyName("inductions")]
    public InductionModel[] Inductions { get; set; } = [];

    [JsonPropertyName("isCompliant")]
    public bool IsCompliant{ get; set; }
}

/// <summary>
/// Model representing an induction record for a worker.
/// </summary>
public class InductionModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the induction record.
    /// </summary>
    [JsonPropertyName("inductionID")]
    public int InductionId { get; set; }
    
    /// <summary>
    /// Gets or sets the UTC date and time when the worker was inducted.
    /// </summary>
    [JsonPropertyName("inductedOnUtc")]
    public DateTime InductedOnUtc { get; set; }
    
    /// <summary>
    /// Gets or sets the UTC date and time when the induction expires.
    /// </summary>
    [JsonPropertyName("expiresOnUtc")]
    public DateTime ExpiresOnUtc { get; set; }
}