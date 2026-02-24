namespace SFA.DAS.AODP.Jobs.Models.Jobs;

[ExcludeFromCodeCoverage]
public class RegulatedJobControl : JobControl
{
    public bool RunApiImport { get; set; }
    public bool ProcessStagingData { get; set; }
}