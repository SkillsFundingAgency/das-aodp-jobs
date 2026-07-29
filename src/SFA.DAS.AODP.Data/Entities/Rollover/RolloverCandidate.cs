using System.ComponentModel.DataAnnotations.Schema;
using SFA.DAS.AODP.Data.Entities.Rollover.Enums;

namespace SFA.DAS.AODP.Data.Entities.Rollover;

[Table("RolloverCandidates", Schema = "dbo")]
public class RolloverCandidate
{
    public Guid Id { get; private set; }

    public string SourceType { get; private set; } = null!;

    public Guid SourceQualificationId { get; private set; }

    public Guid FundingOfferId { get; private set; }

    public string AcademicYear { get; private set; } = null!;

    public int RolloverRound { get; private set; }

    public Guid? RolloverDecisionRunId { get; private set; }

    public RolloverStatus RolloverStatus { get; private set; }

    public string? ExclusionReason { get; private set; }

    public DateTime? PreviousFundingEndDate { get; private set; }

    public DateTime? NewFundingEndDate { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public string? ReviewedByUsername { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public virtual FundingOffer FundingOffer { get; set; } = null!;

    public static RolloverCandidate CreateInitialRound(
        string sourceType,
        Guid sourceQualificationId,
        Guid fundingOfferId,
        string academicYear,
        DateTime createdAt,
        DateOnly? previousFundingEndDate)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentNullException(nameof(sourceType));
        }

        if (string.IsNullOrWhiteSpace(academicYear))
        {
            throw new ArgumentNullException(nameof(academicYear));
        }

        return new RolloverCandidate
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            SourceQualificationId = sourceQualificationId,
            FundingOfferId = fundingOfferId,
            AcademicYear = academicYear,
            RolloverRound = 1,
            RolloverStatus = RolloverStatus.NeedsReview,
            PreviousFundingEndDate = previousFundingEndDate?.ToDateTime(TimeOnly.MinValue),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            IsActive = true
        };
    }
    public static RolloverCandidate CreateInitialRound(
        Guid qualificationVersionId,
        Guid fundingOfferId,
        string academicYear,
        DateTime createdAt,
        DateOnly? previousFundingEndDate)
    {
        return CreateInitialRound(
            RolloverSourceTypes.Ofqual,
            qualificationVersionId,
            fundingOfferId,
            academicYear,
            createdAt,
            previousFundingEndDate);
    }

    public void RefreshFunding(DateOnly? fundingEndDate, DateTime updatedAt)
    {
        PreviousFundingEndDate = fundingEndDate?.ToDateTime(TimeOnly.MinValue);
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    public void Reactivate(DateOnly? fundingEndDate, DateTime updatedAt)
    {
        IsActive = true;
        RolloverStatus = RolloverStatus.NeedsReview;
        ExclusionReason = null;
        PreviousFundingEndDate = fundingEndDate?.ToDateTime(TimeOnly.MinValue);
        NewFundingEndDate = null;
        RolloverDecisionRunId = null;
        ReviewedAt = null;
        ReviewedByUsername = null;
        UpdatedAt = updatedAt;
    }
}
