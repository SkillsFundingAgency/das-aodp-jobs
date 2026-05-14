namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class RegulatedQaaQualificationTests : UnitTest
{
    private const string TestAimCode = "Z1234567";
    private const string TestQualificationTitle = "Access to Higher Education Diploma (Science)";
    private const string TestAwardingBody = "Test Awarding Body";
    private readonly DateTime _testSnapshot = new(2024, 02, 15);
    private readonly DateOnly _testStartDate = new(2023, 09, 01);
    private readonly DateOnly _testLastRegistrationDate = new(2025, 08, 31);
    private readonly SectorSubjectArea _testSectorSubjectArea = SectorSubjectArea.FromTiers("1", "1");

    [Fact]
    public void Create_WithValidParameters_ReturnsInstanceWithCorrectValuesAndDefaults()
    {
        var qualification = CreateQualification(isDiscontinued: true);

        Assert.Equal(_testSnapshot, qualification.DateOfDataSnapshot);
        Assert.Equal(TestAimCode, qualification.AimCode);
        Assert.Equal(TestQualificationTitle, qualification.QualificationTitle);
        Assert.Equal(TestAwardingBody, qualification.AwardingBody);
        Assert.Equal(_testStartDate, qualification.StartDate);
        Assert.Equal(_testLastRegistrationDate, qualification.LastDateForRegistration);
        Assert.True(qualification.IsDiscontinued);
        Assert.Same(_testSectorSubjectArea, qualification.SectorSubjectArea);
        Assert.Equal("Level 3", qualification.Level);
        Assert.Equal("Access to HE", qualification.Type);
        Assert.Equal("Approved", qualification.Status);
        Assert.Equal(1, qualification.ChangeVersion);
        Assert.Equal(_testSnapshot, qualification.LastChangedAt);
        Assert.NotNull(qualification.ContentHash);
        Assert.Null(qualification.LastFundingApprovalEndDate);
        Assert.Equal(QaaImportComparisonOutcome.New, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, qualification.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, qualification.LastDateForRegistrationChangeType);
        Assert.False(qualification.IsRegistrationDateExtended);
        Assert.False(qualification.IsRegistrationDateBroughtForward);
        Assert.NotEqual(Guid.Empty, qualification.Id);
    }

    [Fact]
    public void HasMaterialQaaChange_WhenLastDateForRegistrationChanges_ReturnsTrue()
    {
        var qualification = CreateQualification();

        var result = qualification.HasMaterialQaaChange(new DateOnly(2026, 08, 31), false);

        Assert.True(result);
    }

    [Fact]
    public void HasMaterialQaaChange_WhenIsDiscontinuedChanges_ReturnsTrue()
    {
        var qualification = CreateQualification();

        var result = qualification.HasMaterialQaaChange(_testLastRegistrationDate, true);

        Assert.True(result);
    }

    [Fact]
    public void HasMaterialQaaChange_WhenQualificationTitleChangesOnly_ReturnsFalse()
    {
        var qualification = CreateQualification();

        qualification.ApplyImportedQaaData(
            new DateTime(2024, 03, 15),
            "Updated title",
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            null,
            _testSectorSubjectArea,
            null,
            new DateTime(2024, 03, 15));

        var result = qualification.HasMaterialQaaChange(_testLastRegistrationDate, false);

        Assert.False(result);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenOnlyDescriptiveFieldsChange_DoesNotChangeChangeVersion()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var originalContentHash = qualification.ContentHash;
        var originalLastChangedAt = qualification.LastChangedAt;
        var newSnapshot = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            newSnapshot,
            "Updated title",
            "Updated awarding body",
            new DateOnly(2024, 09, 01),
            _testLastRegistrationDate,
            null,
            SectorSubjectArea.FromTiers("4", "1"),
            null,
            newSnapshot);

        Assert.Equal("Updated title", qualification.QualificationTitle);
        Assert.Equal("Updated awarding body", qualification.AwardingBody);
        Assert.Equal(new DateOnly(2024, 09, 01), qualification.StartDate);
        Assert.Equal(SectorSubjectArea.Engineering, qualification.SectorSubjectArea);
        Assert.Equal(newSnapshot, qualification.DateOfDataSnapshot);
        Assert.Equal(7, qualification.ChangeVersion);
        Assert.Equal(originalLastChangedAt, qualification.LastChangedAt);
        Assert.Equal(originalContentHash, qualification.ContentHash);
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, qualification.PublicationStatus);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationChanges_UpdatesChangeVersion()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var originalContentHash = qualification.ContentHash;
        var changedAt = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2026, 08, 31),
            null,
            _testSectorSubjectArea,
            8,
            changedAt);

        Assert.Equal(8, qualification.ChangeVersion);
        Assert.Equal(changedAt, qualification.LastChangedAt);
        Assert.NotEqual(originalContentHash, qualification.ContentHash);
        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, qualification.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.Extended, qualification.LastDateForRegistrationChangeType);
        Assert.True(qualification.IsRegistrationDateExtended);
        Assert.False(qualification.IsRegistrationDateBroughtForward);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenIsDiscontinuedChanges_UpdatesChangeVersion()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var originalContentHash = qualification.ContentHash;
        var changedAt = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            new DateOnly(2024, 01, 31),
            _testSectorSubjectArea,
            8,
            changedAt);

        Assert.True(qualification.IsDiscontinued);
        Assert.Equal(new DateOnly(2024, 01, 31), qualification.DiscontinuedDate);
        Assert.Equal(8, qualification.ChangeVersion);
        Assert.Equal(changedAt, qualification.LastChangedAt);
        Assert.NotEqual(originalContentHash, qualification.ContentHash);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenMaterialChangeExistsWithoutChangeVersion_Throws()
    {
        var qualification = CreateQualification(changeVersion: 7);

        var exception = Assert.Throws<InvalidOperationException>(() => qualification.ApplyImportedQaaData(
            new DateTime(2024, 03, 15),
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2026, 08, 31),
            null,
            _testSectorSubjectArea,
            null,
            new DateTime(2024, 03, 15)));

        Assert.Equal("A material QAA change requires a change version.", exception.Message);
    }

    [Fact]
    public void MarkSnapshotSeen_UpdatesDateOfDataSnapshotOnly()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var originalContentHash = qualification.ContentHash;
        var originalLastChangedAt = qualification.LastChangedAt;
        var newSnapshotDate = new DateTime(2024, 03, 15);

        qualification.MarkSnapshotSeen(newSnapshotDate);

        Assert.Equal(newSnapshotDate, qualification.DateOfDataSnapshot);
        Assert.Equal(7, qualification.ChangeVersion);
        Assert.Equal(originalLastChangedAt, qualification.LastChangedAt);
        Assert.Equal(originalContentHash, qualification.ContentHash);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationIsBroughtForward_RecordsMovement()
    {
        var qualification = CreateQualification(changeVersion: 7);
        qualification.MarkAsPublished(new DateTime(2024, 02, 20));
        var changedAt = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2024, 08, 31),
            null,
            _testSectorSubjectArea,
            8,
            changedAt);

        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingChange, qualification.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.BroughtForward, qualification.LastDateForRegistrationChangeType);
        Assert.False(qualification.IsRegistrationDateExtended);
        Assert.True(qualification.IsRegistrationDateBroughtForward);
    }

    [Fact]
    public void MarkAsPublished_RecordsPublishedVersionWithoutChangingImportComparisonOutcome()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var publishedAt = new DateTime(2024, 03, 15);

        qualification.MarkAsPublished(publishedAt);

        Assert.Equal(QaaPublicationStatus.Published, qualification.PublicationStatus);
        Assert.Equal(publishedAt, qualification.LastPublishedAt);
        Assert.Equal(7, qualification.LastPublishedChangeVersion);
        Assert.Equal(QaaImportComparisonOutcome.New, qualification.LatestImportComparisonOutcome);
    }

    [Fact]
    public void SetLastFundingApprovalEndDate_DoesNotChangeChangeVersionOrContentHash()
    {
        var qualification = CreateQualification(changeVersion: 7);
        var originalContentHash = qualification.ContentHash;
        var originalLastChangedAt = qualification.LastChangedAt;

        qualification.SetLastFundingApprovalEndDate(new DateTime(2025, 07, 31));

        Assert.Equal(new DateTime(2025, 07, 31), qualification.LastFundingApprovalEndDate);
        Assert.Equal(7, qualification.ChangeVersion);
        Assert.Equal(originalLastChangedAt, qualification.LastChangedAt);
        Assert.Equal(originalContentHash, qualification.ContentHash);
    }

    private RegulatedQaaQualification CreateQualification(bool isDiscontinued = false, long changeVersion = 1)
    {
        return RegulatedQaaQualification.Create(
            _testSnapshot,
            TestAimCode,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            _testSectorSubjectArea,
            isDiscontinued ? new DateOnly(2024, 01, 31) : null,
            changeVersion,
            _testSnapshot);
    }
}
