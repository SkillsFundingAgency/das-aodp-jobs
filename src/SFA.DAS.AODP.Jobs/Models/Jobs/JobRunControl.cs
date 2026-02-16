namespace SFA.DAS.AODP.Jobs.Models.Jobs;

public class JobRunControl
{
    public Guid Id;
    public string Status = string.Empty;
    public DateTime StartTime;
    public DateTime? EndTime;
    public string User = string.Empty;
    public int? RecordsProcessed;
    public Guid JobId;
}