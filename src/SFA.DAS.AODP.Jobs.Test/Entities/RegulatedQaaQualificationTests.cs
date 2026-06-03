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
        Assert.Equal(new DateOnly(2024, 01, 31), qualification.DiscontinuedDate);
        Assert.Same(_testSectorSubjectArea, qualification.SectorSubjectArea);
        Assert.Equal("Level 3", qualification.Level);
        Assert.Equal("Access to HE", qualification.Type);
        Assert.Equal("Approved", qualification.Status);
        Assert.Equal(_testSnapshot, qualification.LastChangedAt);
        Assert.NotNull(qualification.ContentHash);
        Assert.Null(qualification.LastFundingApprovalEndDate);
        Assert.Null(qualification.LatestQaaQualificationHistoryId);
        Assert.Equal(QaaImportComparisonOutcome.New, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, qualification.LastDateForRegistrationChangeType);
        Assert.Equal(_testSnapshot, qualification.FirstSeenAt);
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
    public void ApplyImportedQaaData_WhenOnlyDescriptiveFieldsChange_DoesNotChangeMaterialState()
    {
        var qualification = CreateQualification();
        var historyId = Guid.NewGuid();
        qualification.RecordLatestQaaHistory(historyId);
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
            newSnapshot);

        Assert.Equal("Updated title", qualification.QualificationTitle);
        Assert.Equal("Updated awarding body", qualification.AwardingBody);
        Assert.Equal(new DateOnly(2024, 09, 01), qualification.StartDate);
        Assert.Equal(SectorSubjectArea.Engineering, qualification.SectorSubjectArea);
        Assert.Equal(newSnapshot, qualification.DateOfDataSnapshot);
        Assert.Equal(originalLastChangedAt, qualification.LastChangedAt);
        Assert.Equal(originalContentHash, qualification.ContentHash);
        Assert.Equal(historyId, qualification.LatestQaaQualificationHistoryId);
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, qualification.LastDateForRegistrationChangeType);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationIsExtended_RecordsMaterialChange()
    {
        var qualification = CreateQualification();
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
            changedAt);

        Assert.Equal(changedAt, qualification.LastChangedAt);
        Assert.NotEqual(originalContentHash, qualification.ContentHash);
        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.Extended, qualification.LastDateForRegistrationChangeType);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenIsDiscontinuedChanges_RecordsMaterialChange()
    {
        var qualification = CreateQualification();
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
            changedAt);

        Assert.True(qualification.IsDiscontinued);
        Assert.Equal(new DateOnly(2024, 01, 31), qualification.DiscontinuedDate);
        Assert.Equal(changedAt, qualification.LastChangedAt);
        Assert.NotEqual(originalContentHash, qualification.ContentHash);
        Assert.Equal(QaaImportComparisonOutcome.Discontinued, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, qualification.LastDateForRegistrationChangeType);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenAlreadyDiscontinuedAndMaterialFieldsAreUnchanged_RecordsUnchanged()
    {
        var qualification = CreateQualification(isDiscontinued: true);
        var changedAt = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            new DateOnly(2024, 01, 31),
            _testSectorSubjectArea,
            changedAt);

        Assert.True(qualification.IsDiscontinued);
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, qualification.LatestImportComparisonOutcome);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationIsBroughtForward_RecordsMovement()
    {
        var qualification = CreateQualification();
        var changedAt = new DateTime(2024, 03, 15);

        qualification.ApplyImportedQaaData(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2024, 08, 31),
            null,
            _testSectorSubjectArea,
            changedAt);

        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, qualification.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.BroughtForward, qualification.LastDateForRegistrationChangeType);
    }

    [Fact]
    public void SetLastFundingApprovalEndDate_DoesNotChangeMaterialState()
    {
        var qualification = CreateQualification();
        var originalContentHash = qualification.ContentHash;
        var originalLastChangedAt = qualification.LastChangedAt;

        qualification.SetLastFundingApprovalEndDate(new DateTime(2025, 07, 31));

        Assert.Equal(new DateTime(2025, 07, 31), qualification.LastFundingApprovalEndDate);
        Assert.Equal(originalLastChangedAt, qualification.LastChangedAt);
        Assert.Equal(originalContentHash, qualification.ContentHash);
    }

    private RegulatedQaaQualification CreateQualification(bool isDiscontinued = false)
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
            _testSnapshot);
    }
}
