using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

/// <summary>
/// Represents an import snapshot for QAA data.
/// </summary>
[Table("QaaDataSnapshot", Schema = "regulated")]
public class RegulatedQaaDataSnapshot
{
    public const string StartedStatus = "Started";
    public const string CompletedStatus = "Completed";
    public const string FailedStatus = "Failed";

    private RegulatedQaaDataSnapshot()
    {
    }

    /// <summary>
    /// Gets the unique identifier for the snapshot.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the date and time the snapshot import started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// Gets the date and time the snapshot import completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the status of the snapshot import.
    /// </summary>
    public string Status { get; private set; } = null!;

    /// <summary>
    /// Gets the total number of records processed by the snapshot import.
    /// </summary>
    public int? TotalRecords { get; private set; }

    /// <summary>
    /// Gets the failure reason when the snapshot import failed.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Starts a new QAA data snapshot import.
    /// </summary>
    /// <param name="startedAt">The date and time the snapshot import started.</param>
    /// <returns>The new snapshot.</returns>
    public static RegulatedQaaDataSnapshot Start(DateTime startedAt)
    {
        return new RegulatedQaaDataSnapshot
        {
            Id = Guid.NewGuid(),
            StartedAt = startedAt,
            Status = StartedStatus
        };
    }

    /// <summary>
    /// Completes the snapshot import.
    /// </summary>
    /// <param name="completedAt">The date and time the snapshot import completed.</param>
    /// <param name="totalRecords">The total number of records processed.</param>
    public void Complete(DateTime completedAt, int totalRecords)
    {
        CompletedAt = completedAt;
        TotalRecords = totalRecords;
        Status = CompletedStatus;
        FailureReason = null;
    }

    /// <summary>
    /// Marks the snapshot import as failed.
    /// </summary>
    /// <param name="failedAt">The date and time the snapshot import failed.</param>
    /// <param name="failureReason">The reason the snapshot import failed.</param>
    public void Fail(DateTime failedAt, string failureReason)
    {
        CompletedAt = failedAt;
        Status = FailedStatus;
        FailureReason = failureReason;
    }
}
