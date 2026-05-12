namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class RegulatedQaaDataSnapshotTests
{
    [Fact]
    public void Start_CreatesStartedSnapshot()
    {
        var startedAt = new DateTime(2024, 02, 15);

        var snapshot = RegulatedQaaDataSnapshot.Start(startedAt);

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(startedAt, snapshot.StartedAt);
        Assert.Equal(RegulatedQaaDataSnapshot.StartedStatus, snapshot.Status);
        Assert.Null(snapshot.CompletedAt);
        Assert.Null(snapshot.TotalRecords);
        Assert.Null(snapshot.FailureReason);
    }

    [Fact]
    public void Complete_SetsCompletedSnapshotValues()
    {
        var snapshot = RegulatedQaaDataSnapshot.Start(new DateTime(2024, 02, 15));
        var completedAt = new DateTime(2024, 02, 16);

        snapshot.Complete(completedAt, 12);

        Assert.Equal(completedAt, snapshot.CompletedAt);
        Assert.Equal(12, snapshot.TotalRecords);
        Assert.Equal(RegulatedQaaDataSnapshot.CompletedStatus, snapshot.Status);
        Assert.Null(snapshot.FailureReason);
    }

    [Fact]
    public void Fail_SetsFailedSnapshotValues()
    {
        var snapshot = RegulatedQaaDataSnapshot.Start(new DateTime(2024, 02, 15));
        var failedAt = new DateTime(2024, 02, 16);

        snapshot.Fail(failedAt, "Import failed");

        Assert.Equal(failedAt, snapshot.CompletedAt);
        Assert.Equal(RegulatedQaaDataSnapshot.FailedStatus, snapshot.Status);
        Assert.Equal("Import failed", snapshot.FailureReason);
    }
}
