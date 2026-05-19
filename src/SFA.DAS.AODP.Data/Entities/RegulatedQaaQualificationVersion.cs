using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualificationVersion", Schema = "regulated")]
public class RegulatedQaaQualificationVersion
{
    private RegulatedQaaQualificationVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid QaaQualificationId { get; private set; }

    public string AimCode { get; private set; } = null!;

    public long ChangeVersion { get; private set; }

    public DateOnly DateOfDataSnapshot { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string ContentHash { get; private set; } = null!;

    public string QualificationTitle { get; private set; } = null!;

    public string AwardingBody { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly LastDateForRegistration { get; private set; }

    public bool IsDiscontinued { get; private set; }

    public DateOnly? DiscontinuedDate { get; private set; }

    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

    public string LastDateForRegistrationChangeType { get; private set; } = null!;

    public static RegulatedQaaQualificationVersion Create(
        RegulatedQaaQualification qualification,
        string lastDateForRegistrationChangeType)
    {
        return new RegulatedQaaQualificationVersion
        {
            Id = Guid.NewGuid(),
            QaaQualificationId = qualification.Id,
            AimCode = qualification.AimCode,
            ChangeVersion = qualification.ChangeVersion,
            DateOfDataSnapshot = qualification.DateOfDataSnapshot,
            ChangedAt = qualification.LastChangedAt,
            ContentHash = qualification.ContentHash,
            QualificationTitle = qualification.QualificationTitle,
            AwardingBody = qualification.AwardingBody,
            StartDate = qualification.StartDate,
            LastDateForRegistration = qualification.LastDateForRegistration,
            IsDiscontinued = qualification.IsDiscontinued,
            DiscontinuedDate = qualification.DiscontinuedDate,
            SectorSubjectArea = qualification.SectorSubjectArea,
            LastDateForRegistrationChangeType = lastDateForRegistrationChangeType
        };
    }
}
