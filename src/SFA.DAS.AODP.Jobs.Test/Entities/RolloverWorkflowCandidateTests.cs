using SFA.DAS.AODP.Data.Entities.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class RolloverWorkflowCandidateTests : UnitTest
{
    [Fact]
    public void Invalidate_WhenAlreadyInvalidated_DoesNotOverwriteTheOriginalReasonOrTimestamp()
    {
        var workflowCandidate = new RolloverWorkflowCandidate();
        var firstInvalidatedAt = new DateTime(2026, 6, 1);
        workflowCandidate.Invalidate("First reason", firstInvalidatedAt);

        workflowCandidate.Invalidate("Second reason", new DateTime(2026, 6, 2));

        Assert.Equal(firstInvalidatedAt, workflowCandidate.InvalidatedAt);
        Assert.Equal("First reason", workflowCandidate.InvalidationReason);
    }
}
