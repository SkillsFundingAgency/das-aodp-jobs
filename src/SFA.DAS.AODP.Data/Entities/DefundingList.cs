namespace SFA.DAS.AODP.Data.Entities;

public partial class DefundingList
{
    public int Id { get; set; }
    public string Qan { get; set; } = null!;
    public string? Title { get; set; }
    public string? AwardingOrganisation { get; set; }
    public string? GuidedLearningHours { get; set; }
    public string? SectorSubjectArea { get; set; }
    public string? RelevantRoute { get; set; }
    public string? FundingOffer { get; set; }
    public bool InScope { get; set; } = true;
    public string? Comments { get; set; }
    public DateTime ImportDate { get; set; }
}