using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Data.Entities;

public interface IFundingDomainEventSource
{
    [NotMapped]
    IReadOnlyCollection<FundingDomainEvent> FundingDomainEvents { get; }

    void ClearFundingDomainEvents();
}

[ExcludeFromCodeCoverage]
public abstract record FundingDomainEvent;

[ExcludeFromCodeCoverage]
public sealed record FundingChangedDomainEvent(
    string SourceType,
    Guid SourceQualificationId,
    Guid FundingOfferId,
    Guid? PreviousSourceQualificationId = null) : FundingDomainEvent;

[ExcludeFromCodeCoverage]
public sealed record QualificationFundingEligibilityChangedDomainEvent(
    string SourceType,
    Guid SourceQualificationId) : FundingDomainEvent;
