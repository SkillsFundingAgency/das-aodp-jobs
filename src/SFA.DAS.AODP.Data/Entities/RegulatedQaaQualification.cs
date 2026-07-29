using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

/// <summary>
/// Represents a Qaa qualification entry.
/// </summary>
[Table("QaaQualification", Schema = "regulated")]
public class RegulatedQaaQualification
{
    private RegulatedQaaQualification()
    {
    }

    /// <summary>
    /// Gets the unique identifier for the instance.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The date which this snapshot of data was loaded.
    /// </summary>
    public DateTime DateOfDataSnapshot { get; private set; }

    /// <summary>
    /// The date and time when this QAA qualification was first known to QFAST.
    /// </summary>
    public DateTime FirstSeenAt { get; private set; }

    /// <summary>
    /// The date and time when material QAA data last changed.
    /// </summary>
    public DateTime LastChangedAt { get; private set; }

    /// <summary>
    /// The latest import-to-import comparison result. This is overwritten on each QAA import.
    /// </summary>
    public QaaImportComparisonOutcome LatestImportComparisonOutcome { get; private set; }

    /// <summary>
    /// The movement direction for the last date for registration in the latest material change.
    /// </summary>
    public QaaLastDateForRegistrationChangeType LastDateForRegistrationChangeType { get; private set; }

    /// <summary>
    /// The unique learning AIM code for the qualification.
    /// </summary>
    public string AimCode { get; private set; } = null!;

    /// <summary>
    /// The qualification title.
    /// </summary>
    public string QualificationTitle { get; private set; } = null!;

    /// <summary>
    /// The awarding body (otherwise known as AVAs) that is delivery the qualification.
    /// </summary>
    public string AwardingBody { get; private set; } = null!;

    /// <summary>
    /// The level for the qualification, for QAA this is always Level 3.
    /// </summary>
    public string Level { get; private set; } = null!;

    /// <summary>
    /// The type of qualification, for QAA this is always 'Access to HE'.
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// The current status of the qualification, as we simply import this data, the status is always Approved.
    /// </summary>
    public string Status { get; private set; } = null!;

    /// <summary>
    /// When the qualification started.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// When the last date for registration is.
    /// </summary>
    public DateOnly LastDateForRegistration { get; private set; }

    /// <summary>
    /// Whether the qualification has been discontinued by QAA.
    /// </summary>
    public bool IsDiscontinued { get; private set; }

    /// <summary>
    /// The discontinued date supplied by QAA, when present.
    /// </summary>
    public DateOnly? DiscontinuedDate { get; private set; }

    /// <summary>
    /// The latest material QAA history row for the current imported state.
    /// </summary>
    public Guid? LatestQaaQualificationHistoryId { get; private set; }

    /// <summary>
    /// A value object representation for the sector subject area.
    /// </summary>
    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

    /// <summary>
    /// Funding records held for this QAA qualification.
    /// </summary>
    public virtual ICollection<QaaQualificationFunding> Fundings { get; private set; } = new List<QaaQualificationFunding>();

    /// <summary>
    /// Creates a new entry.
    /// </summary>
    /// <returns>The newly created entry.</returns>
    public static RegulatedQaaQualification Create(
        DateTime snapshotTakenAt,
        string aimCode,
        string qualificationTitle,
        string awardingBody,
        DateOnly registrationOpenedOn,
        DateOnly registrationClosesOn,
        SectorSubjectArea sectorSubjectArea,
        DateOnly? discontinuedDate,
        DateTime? changedAt = null)
    {
        var isDiscontinued = discontinuedDate.HasValue;

        return new RegulatedQaaQualification
        {
            Id = Guid.NewGuid(),
            DateOfDataSnapshot = snapshotTakenAt,
            FirstSeenAt = changedAt ?? snapshotTakenAt,
            LastChangedAt = changedAt ?? snapshotTakenAt,
            AimCode = aimCode,
            QualificationTitle = qualificationTitle,
            AwardingBody = awardingBody,
            Level = "Level 3",
            Type = "Access to Higher Education",
            Status = "Approved",
            StartDate = registrationOpenedOn,
            LastDateForRegistration = registrationClosesOn,
            IsDiscontinued = isDiscontinued,
            DiscontinuedDate = discontinuedDate,
            SectorSubjectArea = sectorSubjectArea,
            LatestImportComparisonOutcome = QaaImportComparisonOutcome.New,
            LastDateForRegistrationChangeType = QaaLastDateForRegistrationChangeType.NotChanged
        };
    }

    /// <summary>
    /// Creates a new entry from existing funded data.
    /// </summary>
    /// <returns>The newly created entry.</returns>
    public static RegulatedQaaQualification CreateFromExisting(
        DateTime snapshotTakenAt,
        string aimCode,
        string qualificationTitle,
        string awardingBody,
        DateOnly registrationOpenedOn,
        DateOnly registrationClosesOn,
        SectorSubjectArea sectorSubjectArea,
        DateOnly? discontinuedDate,
        DateTime? changedAt = null)
    {
        var isDiscontinued = discontinuedDate.HasValue;

        return new RegulatedQaaQualification
        {
            Id = Guid.NewGuid(),
            DateOfDataSnapshot = snapshotTakenAt,
            FirstSeenAt = changedAt ?? snapshotTakenAt,
            LastChangedAt = changedAt ?? snapshotTakenAt,
            AimCode = aimCode,
            QualificationTitle = qualificationTitle,
            AwardingBody = awardingBody,
            Level = "Level 3",
            Type = "Access to Higher Education",
            Status = "Approved",
            StartDate = registrationOpenedOn,
            LastDateForRegistration = registrationClosesOn,
            IsDiscontinued = isDiscontinued,
            DiscontinuedDate = discontinuedDate,
            SectorSubjectArea = sectorSubjectArea,
            LatestImportComparisonOutcome = QaaImportComparisonOutcome.NotChanged,
            LastDateForRegistrationChangeType = QaaLastDateForRegistrationChangeType.NotChanged
        };
    }

    /// <summary>
    /// Determines whether the proposed import contains a material QAA change.
    /// </summary>
    /// <param name="lastDateForRegistration">The proposed last date for registration.</param>
    /// <returns>True if the proposed material data differs from the current material data.</returns>
    public bool AnyChanges(DateOnly lastDateForRegistration)
    {
        _ = HasLastDateForRegistrationChanged(lastDateForRegistration, out var changed) is QaaLastDateForRegistrationChangeType.BroughtForward or QaaLastDateForRegistrationChangeType.Extended;
        return changed;
    }

    /// <summary>
    /// Updates the qualification with the latest Qaa data.
    /// </summary>
    public bool Update(
        DateTime snapshotTakenAt,
        string latestQualificationTitle,
        string latestAwardingBody,
        DateOnly registrationOpenedOn,
        DateOnly registrationClosesOn,
        DateOnly? discontinuedDate,
        SectorSubjectArea sectorSubjectArea,
        DateTime changedAt)
    {
        var wasDiscontinued = IsDiscontinued;
        var qaaHasDiscontinuedQualification = discontinuedDate.HasValue;
        var lastDateForRegistrationChangeType = HasLastDateForRegistrationChanged(registrationClosesOn, out var changed);
        var latestImportComparisonOutcome = !wasDiscontinued && qaaHasDiscontinuedQualification
            ? QaaImportComparisonOutcome.Discontinued
            : changed ? QaaImportComparisonOutcome.LastDateForRegistrationChanged : QaaImportComparisonOutcome.NotChanged;

        RememberLatestQaaDetails(
            snapshotTakenAt,
            latestQualificationTitle,
            latestAwardingBody,
            registrationOpenedOn,
            registrationClosesOn,
            discontinuedDate,
            sectorSubjectArea);

        LatestImportComparisonOutcome = latestImportComparisonOutcome;

        if (!changed)
        {
            LastDateForRegistrationChangeType = QaaLastDateForRegistrationChangeType.NotChanged;
            return changed;
        }

        RecordMaterialQaaChange(
            changedAt,
            latestImportComparisonOutcome,
            lastDateForRegistrationChangeType);

        return changed;
    }

    /// <summary>
    /// Marks that this qualification was included in the latest QAA snapshot.
    /// </summary>
    /// <param name="dateOfDataSnapshot">The date and time of the snapshot.</param>
    public void MarkSnapshotSeen(DateTime dateOfDataSnapshot)
    {
        DateOfDataSnapshot = dateOfDataSnapshot;
    }

    /// <summary>
    /// Links the current material state to its history row.
    /// </summary>
    public void RecordHistory(Guid historyId)
    {
        LatestQaaQualificationHistoryId = historyId;
    }

    private void RememberLatestQaaDetails(
        DateTime snapshotTakenAt,
        string latestQualificationTitle,
        string latestAwardingBody,
        DateOnly registrationOpenedOn,
        DateOnly registrationClosesOn,
        DateOnly? discontinuedDate,
        SectorSubjectArea sectorSubjectArea)
    {
        DateOfDataSnapshot = snapshotTakenAt;
        QualificationTitle = latestQualificationTitle;
        AwardingBody = latestAwardingBody;
        StartDate = registrationOpenedOn;
        LastDateForRegistration = registrationClosesOn;
        IsDiscontinued = discontinuedDate.HasValue;
        DiscontinuedDate = discontinuedDate;
        SectorSubjectArea = sectorSubjectArea;
    }

    private void RecordMaterialQaaChange(
        DateTime changedAt,
        QaaImportComparisonOutcome latestImportComparisonOutcome,
        QaaLastDateForRegistrationChangeType lastDateForRegistrationChangeType)
    {
        LastChangedAt = changedAt;
        LatestImportComparisonOutcome = latestImportComparisonOutcome;
        LastDateForRegistrationChangeType = lastDateForRegistrationChangeType;
    }

    private QaaLastDateForRegistrationChangeType HasLastDateForRegistrationChanged(DateOnly proposedLastDateForRegistration, out bool changed)
    {
        changed = false;
        if (proposedLastDateForRegistration > LastDateForRegistration)
        {
            changed = true;
            return QaaLastDateForRegistrationChangeType.Extended;
        }

        if (proposedLastDateForRegistration < LastDateForRegistration)
        {
            changed = true;
            return QaaLastDateForRegistrationChangeType.BroughtForward;
        }

        return QaaLastDateForRegistrationChangeType.NotChanged;
    }
}
