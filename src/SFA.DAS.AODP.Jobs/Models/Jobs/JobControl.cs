namespace SFA.DAS.AODP.Jobs.Models.Jobs;

public class JobControl
{
    public Guid JobId { get; set; }
    public Guid JobRunId { get; set; }
    public bool JobEnabled { get; set; }
    public string Status { get; set; } = string.Empty;
}