namespace SFA.DAS.AODP.Jobs.Models.Jobs;

[ExcludeFromCodeCoverage]
public class FundedJobControl : JobControl
{
    public bool ImportFundedCsv { get; set; }
    public bool ImportArchivedCsv { get; set; }
}