using Shouldly;

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

        qualification.DateOfDataSnapshot.ShouldBe(_testSnapshot);
        qualification.AimCode.ShouldBe(TestAimCode);
        qualification.QualificationTitle.ShouldBe(TestQualificationTitle);
        qualification.AwardingBody.ShouldBe(TestAwardingBody);
        qualification.StartDate.ShouldBe(_testStartDate);
        qualification.LastDateForRegistration.ShouldBe(_testLastRegistrationDate);

        qualification.IsDiscontinued.ShouldBeTrue();
        qualification.DiscontinuedDate.ShouldBe(new DateOnly(2024, 01, 31));

        qualification.SectorSubjectArea.ShouldBeSameAs(_testSectorSubjectArea);

        qualification.Level.ShouldBe("Level 3");
        qualification.Type.ShouldBe("Access to Higher Education");
        qualification.Status.ShouldBe("Approved");

        qualification.LastChangedAt.ShouldBe(_testSnapshot);

        qualification.Age1619FundingApprovalEndDate.ShouldBeNull();
        qualification.AdvancedLearnerLoansFundingApprovalEndDate.ShouldBeNull();
        qualification.LegalEntitlementL2L3FundingApprovalEndDate.ShouldBeNull();
        qualification.LatestQaaQualificationHistoryId.ShouldBeNull();

        qualification.LatestImportComparisonOutcome.ShouldBe(QaaImportComparisonOutcome.New);
        qualification.LastDateForRegistrationChangeType.ShouldBe(QaaLastDateForRegistrationChangeType.NotChanged);

        qualification.FirstSeenAt.ShouldBe(_testSnapshot);

        qualification.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void HasMaterialQaaChange_WhenLastDateForRegistrationChanges_ReturnsTrue()
    {
        var qualification = CreateQualification();

        var result = qualification.AnyChanges(new DateOnly(2026, 08, 31));

        result.ShouldBeTrue();
    }

    [Fact]
    public void ApplyImportedQaaData_WhenOnlyDescriptiveFieldsChange_DoesNotChangeMaterialState()
    {
        var qualification = CreateQualification();
        var historyId = Guid.NewGuid();
        qualification.RecordHistory(historyId);
        var originalLastChangedAt = qualification.LastChangedAt;
        var newSnapshot = new DateTime(2024, 03, 15);

        qualification.Update(
            newSnapshot,
            "Updated title",
            "Updated awarding body",
            new DateOnly(2024, 09, 01),
            _testLastRegistrationDate,
            null,
            SectorSubjectArea.FromTiers("4", "1"),
            newSnapshot);

        qualification.QualificationTitle.ShouldBe("Updated title");
        qualification.AwardingBody.ShouldBe("Updated awarding body");
        qualification.StartDate.ShouldBe(new DateOnly(2024, 09, 01));
        qualification.SectorSubjectArea.ShouldBe(SectorSubjectArea.Engineering);

        qualification.DateOfDataSnapshot.ShouldBe(newSnapshot);
        qualification.LastChangedAt.ShouldBe(originalLastChangedAt);

        qualification.LatestQaaQualificationHistoryId.ShouldBe(historyId);
        qualification.LatestImportComparisonOutcome.ShouldBe(QaaImportComparisonOutcome.NotChanged);
        qualification.LastDateForRegistrationChangeType.ShouldBe(QaaLastDateForRegistrationChangeType.NotChanged);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationIsExtended_RecordsMaterialChange()
    {
        var qualification = CreateQualification();
        var changedAt = new DateTime(2024, 03, 15);

        qualification.Update(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2026, 08, 31),
            null,
            _testSectorSubjectArea,
            changedAt);

        qualification.LastChangedAt.ShouldBe(changedAt);
        qualification.LatestImportComparisonOutcome.ShouldBe(QaaImportComparisonOutcome.LastDateForRegistrationChanged);
        qualification.LastDateForRegistrationChangeType.ShouldBe(QaaLastDateForRegistrationChangeType.Extended);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenAlreadyDiscontinuedAndMaterialFieldsAreUnchanged_RecordsUnchanged()
    {
        var qualification = CreateQualification(isDiscontinued: true);
        var changedAt = new DateTime(2024, 03, 15);

        qualification.Update(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            new DateOnly(2024, 01, 31),
            _testSectorSubjectArea,
            changedAt);

        qualification.IsDiscontinued.ShouldBeTrue();
        qualification.LatestImportComparisonOutcome.ShouldBe(QaaImportComparisonOutcome.NotChanged);
    }

    [Fact]
    public void ApplyImportedQaaData_WhenLastDateForRegistrationIsBroughtForward_RecordsMovement()
    {
        var qualification = CreateQualification();
        var changedAt = new DateTime(2024, 03, 15);

        qualification.Update(
            changedAt,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            new DateOnly(2024, 08, 31),
            null,
            _testSectorSubjectArea,
            changedAt);

        qualification.LastDateForRegistrationChangeType.ShouldBe(QaaLastDateForRegistrationChangeType.BroughtForward);
        qualification.LatestImportComparisonOutcome.ShouldBe(QaaImportComparisonOutcome.LastDateForRegistrationChanged);
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
