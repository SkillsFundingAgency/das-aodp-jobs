using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities.Rollover;

[Table("RolloverWorkflowCandidate", Schema = "dbo")]
public class RolloverWorkflowCandidate
{
    public Guid Id { get; private set; }

    public Guid RolloverCandidatesId { get; private set; }

    public DateTime? InvalidatedAt { get; private set; }

    public string? InvalidationReason { get; private set; }

    public void Invalidate(string reason, DateTime invalidatedAt)
    {
        if (InvalidatedAt.HasValue)
        {
            return;
        }

        InvalidatedAt = invalidatedAt;
        InvalidationReason = reason;
    }
}
