using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualification", Schema = "regulated")]
public class RegulatedQaaQualification
{
    public Guid Id { get; private set; }

    public DateTime DateOfDataSnapshot { get; private set; }

    public string AimCode { get; private set; } = null!;

    public string QualificationTitle { get; private set; } = null!;

    public string AwardingBody { get; private set; } = null!;

    public string Level { get; private set; } = null!;

    public string Type { get; private set; } = null!;

    public string Status { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly LastDateForRegistration { get; private set; }

    public DateTime? LastFundingApprovalEndDate { get; private set; }

    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

    public static RegulatedQaaQualification Create(
        DateTime dateOfDataSnapshot,
        string aimCode,
        string qualificationTitle,
        string awardingBody,
        DateOnly startDateForRegistration, 
        DateOnly lastDateForRegistration, 
        SectorSubjectArea sectorSubjectArea)
    {
        return new RegulatedQaaQualification
        {
            DateOfDataSnapshot = dateOfDataSnapshot,
            AimCode = aimCode,
            QualificationTitle = qualificationTitle,
            AwardingBody = awardingBody,
            Level = "Level 3",
            Type = "Access to HE",
            Status = "Approved",
            StartDate = startDateForRegistration,
            LastDateForRegistration = lastDateForRegistration,
            SectorSubjectArea = sectorSubjectArea
        };
    }
}