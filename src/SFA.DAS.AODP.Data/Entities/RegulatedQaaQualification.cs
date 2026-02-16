using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualification", Schema = "regulated")]
public class RegulatedQaaQualification
{
    public Guid Id { get; private set; }

    public string AimCode { get; private set; }

    public string QualificationTitle { get; private set; }

    public string AwardingBody { get; private set; }

    public string Level { get; private set; } = null!;

    public string Type { get; private set; } = null!;

    public string Status { get; private set; } = null!;

    public DateTime StartDate { get; private set; }

    public DateTime LastDateForRegistration { get; private set; }

    public DateTime? LastFundingApprovalEndDate { get; private set; }

    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

    public static RegulatedQaaQualification Create(
        string aimCode,
        string qualificationTitle,
        string awardingBody,
        DateTime startDate, 
        DateTime lastDateForRegistration, 
        SectorSubjectArea sectorSubjectArea)
    {
        return new RegulatedQaaQualification
        {
            AimCode = aimCode,
            QualificationTitle = qualificationTitle,
            AwardingBody = awardingBody,
            Level = "Level 3",
            Type = "Access to HE",
            Status = "Approved",
            StartDate = startDate,
            LastDateForRegistration = lastDateForRegistration,
            SectorSubjectArea = sectorSubjectArea
        };
    }
}