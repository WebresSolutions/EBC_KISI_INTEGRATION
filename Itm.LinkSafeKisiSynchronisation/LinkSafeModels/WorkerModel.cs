namespace Itm.LinkSafeKisiSynchronisation.LinkSafeModels;

public class WorkersModel
{
    public WorkerModel[] Workers { get; set; }
}

public class WorkerModel
{
    public int WorkerId { get; set; }
    public string EmailAddress { get; set; }
    public InductionModel[] Inductions { get; set; }
}

public class InductionModel
{
    public int InductionId { get; set; }
    public DateTime InductedOnUtc { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
}