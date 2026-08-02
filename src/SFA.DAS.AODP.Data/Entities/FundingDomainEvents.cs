using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

public interface IFundingDomainEventSource
{
    [NotMapped]
    IReadOnlyCollection<FundingDomainEvent> FundingDomainEvents { get; }

    void ClearFundingDomainEvents();
}

public abstract record FundingDomainEvent;

public sealed record FundingChangedDomainEvent(
    string SourceType,
    Guid SourceQualificationId,
    Guid FundingOfferId,
    Guid? PreviousSourceQualificationId = null) : FundingDomainEvent;

public sealed record QualificationFundingEligibilityChangedDomainEvent(
    string SourceType,
    Guid SourceQualificationId) : FundingDomainEvent;
