using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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
    /// The date and time when this QAA qualification was first known to AODP.
    /// </summary>
    public DateTime FirstSeenAt { get; private set; }

    /// <summary>
    /// The date and time when material QAA data last changed.
    /// </summary>
    public DateTime LastChangedAt { get; private set; }

    /// <summary>
    /// The hash of the material QAA data.
    /// </summary>
    public string ContentHash { get; private set; } = null!;

    /// <summary>
    /// The latest import-to-import comparison result. This is overwritten on each QAA import.
    /// </summary>
    public string LatestImportComparisonOutcome { get; private set; } = null!;

    /// <summary>
    /// The movement direction for the last date for registration in the latest material change.
    /// </summary>
    public string LastDateForRegistrationChangeType { get; private set; } = null!;

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
    /// What date is the last date that funding can be approved for, this is set as part of the output file generation.
    /// </summary>
    public DateTime? LastFundingApprovalEndDate { get; private set; }

    /// <summary>
    /// The latest material QAA history row for the current imported state.
    /// </summary>
    public Guid? LatestQaaQualificationHistoryId { get; private set; }

    /// <summary>
    /// A value object representation for the sector subject area.
    /// </summary>
    public SectorSubjectArea SectorSubjectArea { get; private set; } = null!;

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
        var materialQaaState = MaterialQaaState.From(registrationClosesOn, isDiscontinued);

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
            Type = "Access to HE",
            Status = "Approved",
            StartDate = registrationOpenedOn,
            LastDateForRegistration = registrationClosesOn,
            IsDiscontinued = isDiscontinued,
            DiscontinuedDate = discontinuedDate,
            SectorSubjectArea = sectorSubjectArea,
            ContentHash = materialQaaState.ContentHash,
            LatestImportComparisonOutcome = QaaImportComparisonOutcome.New,
            LastDateForRegistrationChangeType = QaaLastDateForRegistrationChangeType.NotChanged
        };
    }

    /// <summary>
    /// Determines whether the proposed import contains a material QAA change.
    /// </summary>
    /// <param name="lastDateForRegistration">The proposed last date for registration.</param>
    /// <param name="isDiscontinued">Whether the proposed qualification is discontinued.</param>
    /// <returns>True if the proposed material data differs from the current material data.</returns>
    public bool HasMaterialQaaChange(DateOnly lastDateForRegistration, bool isDiscontinued)
    {
        var proposedQaaState = MaterialQaaState.From(lastDateForRegistration, isDiscontinued);

        return MaterialQaaStateHasChanged(proposedQaaState);
    }

    /// <summary>
    /// Applies the latest imported QAA data.
    /// </summary>
    public void ApplyImportedQaaData(
        DateTime snapshotTakenAt,
        string latestQualificationTitle,
        string latestAwardingBody,
        DateOnly registrationOpenedOn,
        DateOnly registrationClosesOn,
        DateOnly? discontinuedDate,
        SectorSubjectArea sectorSubjectArea,
        DateTime changedAt)
    {
        var qaaHasDiscontinuedQualification = discontinuedDate.HasValue;
        var importedQaaState = MaterialQaaState.From(
            registrationClosesOn,
            qaaHasDiscontinuedQualification);
        var hasMaterialChange = MaterialQaaStateHasChanged(importedQaaState);
        var lastDateForRegistrationChangeType = WorkOutLastDateForRegistrationChangeType(registrationClosesOn);

        RememberLatestQaaDetails(
            snapshotTakenAt,
            latestQualificationTitle,
            latestAwardingBody,
            registrationOpenedOn,
            registrationClosesOn,
            discontinuedDate,
            sectorSubjectArea);

        if (!hasMaterialChange)
        {
            LatestImportComparisonOutcome = QaaImportComparisonOutcome.Unchanged;
            LastDateForRegistrationChangeType = QaaLastDateForRegistrationChangeType.NotChanged;
            return;
        }

        RecordMaterialQaaChange(
            importedQaaState,
            changedAt,
            lastDateForRegistrationChangeType);
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
    /// Sets the last date that funding can be approved for.
    /// </summary>
    /// <param name="lastFundingApprovalEndDate">The last funding approval end date.</param>
    public void SetLastFundingApprovalEndDate(DateTime? lastFundingApprovalEndDate)
    {
        LastFundingApprovalEndDate = lastFundingApprovalEndDate;
    }

    /// <summary>
    /// Links the current material state to its history row.
    /// </summary>
    public void RecordLatestQaaHistory(Guid historyId)
    {
        LatestQaaQualificationHistoryId = historyId;
    }

    private static string GenerateContentHash(DateOnly lastDateForRegistration, bool isDiscontinued)
    {
        var content = string.Join(
            "|",
            lastDateForRegistration.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            isDiscontinued);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private bool MaterialQaaStateHasChanged(MaterialQaaState proposedQaaState)
    {
        return ContentHash != proposedQaaState.ContentHash;
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
        MaterialQaaState importedQaaState,
        DateTime changedAt,
        string lastDateForRegistrationChangeType)
    {
        LastChangedAt = changedAt;
        ContentHash = importedQaaState.ContentHash;
        LatestImportComparisonOutcome = QaaImportComparisonOutcome.MaterialChanged;
        LastDateForRegistrationChangeType = lastDateForRegistrationChangeType;
    }

    private string WorkOutLastDateForRegistrationChangeType(DateOnly proposedLastDateForRegistration)
    {
        if (proposedLastDateForRegistration > LastDateForRegistration)
        {
            return QaaLastDateForRegistrationChangeType.Extended;
        }

        return proposedLastDateForRegistration < LastDateForRegistration
            ? QaaLastDateForRegistrationChangeType.BroughtForward
            : QaaLastDateForRegistrationChangeType.NotChanged;
    }

    private sealed record MaterialQaaState(DateOnly RegistrationClosesOn, bool IsDiscontinued)
    {
        public string ContentHash => GenerateContentHash(RegistrationClosesOn, IsDiscontinued);

        public static MaterialQaaState From(DateOnly registrationClosesOn, bool isDiscontinued)
        {
            return new MaterialQaaState(registrationClosesOn, isDiscontinued);
        }
    }
}
