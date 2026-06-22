using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualificationHistory", Schema = "regulated")]
public class RegulatedQaaQualificationHistory
{
    private RegulatedQaaQualificationHistory()
    {
    }

    public Guid Id { get; private set; }

    public Guid QaaQualificationId { get; private set; }

    public string AimCode { get; private set; } = null!;

    public DateTime DateOfDataSnapshot { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string QualificationTitle { get; private set; } = null!;

    public string AwardingBody { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly LastDateForRegistration { get; private set; }

    public bool IsDiscontinued { get; private set; }

    public DateOnly? DiscontinuedDate { get; private set; }

    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

    public QaaLastDateForRegistrationChangeType LastDateForRegistrationChangeType { get; private set; }

    /// <summary>
    /// The last date that this qualification will be funded to for the Age 16-19 funding stream.
    /// </summary>
    public DateOnly? Age1619FundingApprovalEndDate { get; set; }

    /// <summary>
    /// The last date that this qualification will be funded to for the Advanced Learner Loans funding stream.
    /// </summary>
    public DateOnly? AdvancedLearnerLoansFundingApprovalEndDate { get; set; }

    /// <summary>
    /// The last date that this qualification will be funded to for the Legal entitlement L2-L3 funding stream.
    /// </summary>
    public DateOnly? LegalEntitlementL2L3FundingApprovalEndDate { get; set; }

    public static RegulatedQaaQualificationHistory Create(
        RegulatedQaaQualification qualification,
        QaaLastDateForRegistrationChangeType lastDateForRegistrationChangeType)
    {
        return new RegulatedQaaQualificationHistory
        {
            Id = Guid.NewGuid(),
            QaaQualificationId = qualification.Id,
            AimCode = qualification.AimCode,
            DateOfDataSnapshot = qualification.DateOfDataSnapshot,
            ChangedAt = qualification.LastChangedAt,
            QualificationTitle = qualification.QualificationTitle,
            AwardingBody = qualification.AwardingBody,
            StartDate = qualification.StartDate,
            LastDateForRegistration = qualification.LastDateForRegistration,
            IsDiscontinued = qualification.IsDiscontinued,
            DiscontinuedDate = qualification.DiscontinuedDate,
            SectorSubjectArea = qualification.SectorSubjectArea,
            LastDateForRegistrationChangeType = lastDateForRegistrationChangeType,
            Age1619FundingApprovalEndDate = qualification.Age1619FundingApprovalEndDate,
            AdvancedLearnerLoansFundingApprovalEndDate = qualification.AdvancedLearnerLoansFundingApprovalEndDate,
            LegalEntitlementL2L3FundingApprovalEndDate = qualification.LegalEntitlementL2L3FundingApprovalEndDate
        };
    }
}
