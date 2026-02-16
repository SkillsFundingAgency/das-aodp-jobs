namespace SFA.DAS.AODP.Jobs.Models.Jobs;

public class RegulatedJobControl : JobControl
{
    public bool RunApiImport { get; set; }
    public bool ProcessStagingData { get; set; }
}