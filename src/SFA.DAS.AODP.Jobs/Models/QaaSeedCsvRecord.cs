namespace SFA.DAS.AODP.Jobs.Models;

public record QaaSeedCsvRecord
{
    public string AimCode { get; set; } = null!;

    public string AwardingBody { get; set; } = null!;

    public string DiplomaTitle { get; set; } = null!;

    public string SsaTier1 { get; set; } = null!;

    public string SsaTier2 { get; set; } = null!;

    public string? StartDateOfQualification { get; set; }

    public DateOnly FullStartDateOfQualification { get; set; }

    public string? LastDateForRegistration { get; set; }

    public DateOnly FullLastDateForRegistration { get; set; }

    public string? LastDateForCertification { get; set; }

    public DateOnly FullLastDateForCertification { get; set; }

    public string AwardStatus { get; set; } = null!;

    public DateOnly? DiscontinuedDate { get; set; }
}
