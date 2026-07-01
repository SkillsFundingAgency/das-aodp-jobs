namespace SFA.DAS.AODP.Infrastructure.Models.Rollover;

public class RolloverCandidateFundingStream
{
    public Guid QualificationVersionId { get; init; }

    public Guid FundingOfferId { get; init; }

    public DateOnly? EndDate { get; init; }
}
