using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualificationFundings", Schema = "funded")]
public class QaaQualificationFunding
{
    public Guid Id { get; private set; }

    public Guid QaaQualificationId { get; private set; }

    public Guid FundingOfferId { get; private set; }

    public string AcademicYear { get; private set; } = null!;

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public string? FundingStatus { get; private set; }

    public string? Comments { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static QaaQualificationFunding Create(
        Guid qaaQualificationId,
        Guid fundingOfferId,
        string academicYear,
        DateOnly? startDate,
        DateOnly? endDate,
        string? fundingStatus,
        DateTime createdAt,
        string? comments = null)
    {
        if (qaaQualificationId == Guid.Empty)
        {
            throw new ArgumentException(
                "QAA qualification id must be provided.",
                nameof(qaaQualificationId));
        }

        if (fundingOfferId == Guid.Empty)
        {
            throw new ArgumentException(
                "Funding offer id must be provided.",
                nameof(fundingOfferId));
        }

        if (string.IsNullOrWhiteSpace(academicYear))
        {
            throw new ArgumentNullException(nameof(academicYear));
        }

        return new QaaQualificationFunding
        {
            Id = Guid.NewGuid(),
            QaaQualificationId = qaaQualificationId,
            FundingOfferId = fundingOfferId,
            AcademicYear = academicYear,
            StartDate = startDate,
            EndDate = endDate,
            FundingStatus = fundingStatus,
            Comments = comments,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
