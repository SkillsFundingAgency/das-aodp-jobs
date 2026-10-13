using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QaaQualificationFundings", Schema = "funded")]
public class QaaQualificationFunding : IFundingDomainEventSource
{
    private readonly List<FundingDomainEvent> _fundingDomainEvents = [];

    public Guid Id { get; private set; }

    public Guid QaaQualificationId { get; private set; }

    public Guid FundingOfferId { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public string? FundingStatus { get; private set; }

    public string? Comments { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public virtual RegulatedQaaQualification QaaQualification { get; private set; } = null!;

    [NotMapped]
    public IReadOnlyCollection<FundingDomainEvent> FundingDomainEvents => _fundingDomainEvents;

    public static QaaQualificationFunding Create(
        Guid qaaQualificationId,
        Guid fundingOfferId,
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

        var funding = new QaaQualificationFunding
        {
            Id = Guid.NewGuid(),
            QaaQualificationId = qaaQualificationId,
            FundingOfferId = fundingOfferId,
            StartDate = startDate,
            EndDate = endDate,
            FundingStatus = fundingStatus,
            Comments = comments,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        funding.RecordChanged();
        return funding;
    }

    public void Update(
        DateOnly? startDate,
        DateOnly? endDate,
        string? fundingStatus,
        DateTime updatedAt,
        string? comments = null)
    {
        StartDate = startDate;
        EndDate = endDate;
        FundingStatus = fundingStatus;
        Comments = comments;
        UpdatedAt = updatedAt;
        RecordChanged();
    }

    public void Archive(DateOnly endDate, DateTime updatedAt, string? comments = null)
    {
        EndDate = endDate;
        Comments = comments;
        UpdatedAt = updatedAt;
        RecordChanged();
    }

    public void ClearFundingDomainEvents() => _fundingDomainEvents.Clear();

    private void RecordChanged()
    {
        _fundingDomainEvents.Add(new FundingChangedDomainEvent(
            Rollover.RolloverSourceTypes.Qaa,
            QaaQualificationId,
            FundingOfferId));
    }
}
