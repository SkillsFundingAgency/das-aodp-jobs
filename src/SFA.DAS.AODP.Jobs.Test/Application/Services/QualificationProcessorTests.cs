using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using Shouldly;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QualificationProcessorTests
{
    private readonly Mock<IFundingEligibilityService> _eligibilityMock;
    private readonly Mock<IChangeDetectionService> _changeMock;
    private readonly QualificationProcessor _processor;

    public QualificationProcessorTests()
    {
        _eligibilityMock = new Mock<IFundingEligibilityService>();
        _changeMock = new Mock<IChangeDetectionService>();

        _processor = new QualificationProcessor(
            _eligibilityMock.Object,
            _changeMock.Object
        );
    }

    [Theory]
    [InlineData(true, false, true)]   // Eligible -> Decision Required
    [InlineData(false, true, true)]   // Ineligible + Conflict (Active Apps) -> Decision Required
    [InlineData(false, false, false)] // Ineligible + No Conflict -> No Action Required
    public void Process_NewRecord_Paths(bool isEligible, bool hasActiveApps, bool expectDecisionRequired)
    {
        // Arrange
        var dto = new QualificationDTO { QualificationNumberNoObliques = "12345", Title = "New Qual" };
        var qualId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var expectedStatusId = expectDecisionRequired
            ? ProcessStatusLookup.DecisionRequired.Id
            : ProcessStatusLookup.NoActionRequired.Id;

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(dto))
            .Returns(new FundingEligibilityEvaluation
            {
                Rules = isEligible
                    ? []
                    : [new FundingEligibilityRuleResult("none", false, [])]
            });

        // Act
        var result = _processor.Process(dto, null, qualId, orgId, hasActiveApps, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.NewVersion.Version);
        Assert.Equal(qualId, result.NewVersion.QualificationId);
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);

        var expectedStage = expectDecisionRequired
            ? LifecycleStageLookup.New.Id
            : LifecycleStageLookup.Completed.Id;

        result.NewVersion.LifecycleStageId.ShouldBe(expectedStage);
    }

    [Theory]
    [InlineData(true, false, false, false)]  // Flip to false + no conflict -> No Action Required
    [InlineData(false, true, false, true)]   // App Conflict -> Decision Required
    [InlineData(false, false, true, true)]   // Funding Conflict -> Decision Required
    [InlineData(false, false, false, false)] // Still Ineligible - No Change -> No Action Required
    public void Process_ExistingIneligible_AllPaths(
        bool eligibilityChanged,
        bool hasApps,
        bool hasFunding,
        bool expectDecisionRequired)
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            EligibleForFunding = eligibilityChanged,
            ProcessStatusId = ProcessStatusLookup.NoActionRequired.Id,
            Qualification = new Qualification { Qan = "123" }
        };

        var currentEval = new FundingEligibilityEvaluation
        {
            Rules =
            [
                new FundingEligibilityRuleResult("Glh", false, ["Glh"])
            ]
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(currentEval);

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        var expectedStatusId = expectDecisionRequired
            ? ProcessStatusLookup.DecisionRequired.Id
            : ProcessStatusLookup.NoActionRequired.Id;

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), hasApps, hasFunding);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(expectedStatusId);

        if (expectDecisionRequired)
        {
            result.FieldChange.ChangedFieldNames.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(true, true, true, true)]     // Major change only -> Decision Required
    [InlineData(false, true, false, false)]  // Eligibility dropped -> No Action Required
    [InlineData(false, false, true, true)]   // Eligibility gained -> Decision Required
    [InlineData(false, true, true, false)]   // Minor change, no logic change -> Approved
    public void Process_ExistingApproved_ReflectingRealLogic(
        bool hasKeyChanges,
        bool prevPassed,
        bool currPassed,
        bool expectDecisionRequired)
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = ProcessStatusLookup.Approved.Id,
            EligibleForFunding = prevPassed,
            Qualification = new Qualification { Qan = "123" }
        };

        var changedFields = new List<string>();

        if (hasKeyChanges)
        {
            changedFields.Add(KeyField.Level.ToString());
        }
        
        var currEval = new FundingEligibilityEvaluation
        {
            Rules = currPassed
                ? []
                : [new FundingEligibilityRuleResult("Rule name", false, ["TestField"])]
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(currEval);

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true, ChangedFields = changedFields});

        var expectedStatusId = expectDecisionRequired
            ? ProcessStatusLookup.DecisionRequired.Id
            : (prevPassed && currPassed && !hasKeyChanges
                ? ProcessStatusLookup.Approved.Id
                : ProcessStatusLookup.NoActionRequired.Id);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(expectedStatusId);
    }

    [Theory]
    [InlineData(true, true, "major")]
    [InlineData(false, true, "minor")]
    [InlineData(true, false, "major")]
    [InlineData(false, false, "minor")]
    public void Process_ExistingInReview_MaintainsStatus(
        bool hasKeyChanges,
        bool startsOnHold,
        string expectedNoteWord)
    {
        // Arrange
        var statusId = startsOnHold
            ? ProcessStatusLookup.OnHold.Id
            : ProcessStatusLookup.DecisionRequired.Id;

        var changedFields = new List<string>();

        if (hasKeyChanges)
        {
            changedFields.Add(KeyField.Level.ToString());
        }

        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = statusId,
            LifecycleStageId = LifecycleStageLookup.Changed.Id,
            Version = 1,
            QualificationId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" },
            EligibleForFunding = true
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(new FundingEligibilityEvaluation { Rules = [] });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true, ChangedFields = changedFields});

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, existingVersion.QualificationId, Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(statusId);
        result.Discussion.Notes!.ShouldContain(expectedNoteWord);
    }

    [Fact]
    public void Process_UnknownStatus_DefaultsToNoActionRequired()
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" }
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(new FundingEligibilityEvaluation { Rules = [] });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(ProcessStatusLookup.NoActionRequired.Id);
        result.Discussion.Notes!.ShouldContain("no action required - changed qualification");
    }
}