namespace SFA.DAS.AODP.Infrastructure.Models.Rollover;

public class RolloverCandidateFundingStream
{
    public string SourceType { get; init; } = null!;

    public Guid SourceQualificationId { get; init; }

    public Guid FundingOfferId { get; init; }

    public DateOnly? EndDate { get; init; }
}
