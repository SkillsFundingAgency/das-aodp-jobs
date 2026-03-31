using DocumentFormat.OpenXml.Vml;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;
namespace SFA.DAS.AODP.Jobs.Tests;
public class QualificationProcessorTests
{
    private readonly Mock<IFundingEligibilityService> _eligibilityMock;
    private readonly Mock<IChangeDetectionService> _changeMock;
    private readonly QualificationProcessor _processor;
    private readonly QualificationProcessorSettings _settings;

    public QualificationProcessorTests()
    {
        _eligibilityMock = new Mock<IFundingEligibilityService>();
        _changeMock = new Mock<IChangeDetectionService>();


        _settings = new QualificationProcessorSettings
        {
            ApprovedStatusId = Guid.NewGuid(),
            RejectedStatusId = Guid.NewGuid(),
            OnHoldStatusId = Guid.NewGuid(),
            DecisionRequiredStatusId = Guid.NewGuid(),
            NoActionRequiredStatusId = Guid.NewGuid(),
            NewLifecycleStageId = Guid.NewGuid(),
            ChangedLifecycleStageId = Guid.NewGuid(),
            ActionTypeDecisionId = Guid.NewGuid(),
            ActionTypeNoActionId = Guid.NewGuid()
        };

        _processor = new QualificationProcessor(
            _eligibilityMock.Object,
            _changeMock.Object
        );
    }


    [Theory]
    [InlineData(true, false, "DecisionRequiredStatusId")]   // Path 1: Eligible
    [InlineData(false, true, "DecisionRequiredStatusId")]   // Path 2: Ineligible + Conflict (Active Apps)
    [InlineData(false, false, "NoActionRequiredStatusId")] // Path 3: Ineligible + No Conflict
    public void Process_NewRecord_Paths(bool isEligible, bool hasActiveApps, string expectedStatusProperty)
    {
        // Arrange
        var dto = new QualificationDTO { QualificationNumberNoObliques = "12345", Title = "New Qual" };
        var qualId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // Reflection helper to get the Guid from your _settings object based on the property name string
        var property = _settings.GetType().GetProperty(expectedStatusProperty)
            ?? throw new InvalidOperationException($"Property '{expectedStatusProperty}' not found.");

        var value = property.GetValue(_settings)
            ?? throw new InvalidOperationException($"Property '{expectedStatusProperty}' is null.");

        var expectedStatusId = (Guid)value;

        // Setup Mock based on 'isEligible' parameter
        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(dto))
            .Returns(new FundingEligibilityEvaluation
            {
                Rules = isEligible
                    ? new List<FundingEligibilityRuleResult>() // Empty = Passed
                    : new List<FundingEligibilityRuleResult> { new() { Passed = false } }
            });

        // Act 
        var result = _processor.Process(dto, null, qualId, orgId, hasActiveApps, false, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.NewVersion.Version);
        Assert.Equal(qualId, result.NewVersion.QualificationId);

        // Validate the logic-driven outcomes
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);
        Assert.Equal(result.NewVersion.LifecycleStageId, _settings.NewLifecycleStageId);
    }

    [Theory]
    // Eligibility Changed? | Has Apps? | Has Funding? | Expected Status 
    [InlineData(true, false, false, "DecisionRequiredStatusId")]  // Eligibility changed to false
    [InlineData(false, true, false, "DecisionRequiredStatusId")]  // New App Conflict
    [InlineData(false, false, true, "DecisionRequiredStatusId")]  // New Funding Conflict
    [InlineData(false, false, false, "NoActionRequiredStatusId")] // Still Ineligible - No Change
    public void Process_ExistingIneligible_AllPaths(
        bool eligibilityChanged,
        bool hasApps,
        bool hasFunding,
        string expectedStatusProp)
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = _settings.ApprovedStatusId, // Start as Approved
            Qualification = new Qualification { Qan = "123" }
        };

        // 1. Current state is NOT eligible
        var currentEval = new FundingEligibilityEvaluation { Rules = new() { new() { Passed = false } } };

        // 2. Previous state depends on 'eligibilityChanged'
        var previousEval = new FundingEligibilityEvaluation
        {
            Rules = eligibilityChanged ? new() : new() { new() { Passed = false } }
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>())).Returns(currentEval);
        _eligibilityMock.Setup(s => s.CompareFundingEvaluations(It.IsAny<FundingEligibilityEvaluation>(), It.IsAny<FundingEligibilityEvaluation>()))
            .Returns(new FundingEligibilityComparison
            {
                PreviousEvaluation = previousEval,
                CurrentEvaluation = currentEval
            });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), It.IsAny<QualificationVersions>()))
            .Returns(new DetectionResults { ChangesPresent = true });

        var property = _settings.GetType().GetProperty(expectedStatusProp)
            ?? throw new InvalidOperationException($"Property '{expectedStatusProp}' not found.");

        var value = property.GetValue(_settings)
            ?? throw new InvalidOperationException($"Property '{expectedStatusProp}' is null.");

        var expectedStatusId = (Guid)value;

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), hasApps, hasFunding, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);
        Assert.Equal(_settings.ChangedLifecycleStageId, result.NewVersion.LifecycleStageId);
    }

    [Theory]
    // Scenario: [Key Change], [Prev. Rules Passed], [Curr. Rules Passed], [Expected Status]
    [InlineData(true, true, true, "DecisionRequiredStatusId")] // Major change only
    [InlineData(false, true, false, "DecisionRequiredStatusId")] // Eligibility dropped (Pass -> Fail)
    [InlineData(false, false, true, "DecisionRequiredStatusId")] // Eligibility gained (Fail -> Pass)
    [InlineData(false, true, true, "ApprovedStatusId")]         // Minor change, no logic change
    public void Process_ExistingApproved_ReflectingRealLogic(bool hasKeyChanges, bool prevPassed, bool currPassed, string expectedStatusProp)
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = _settings.ApprovedStatusId,
            Qualification = new Qualification { Qan = "123" }
        };

        // We set the Rules here. 
        // Your FundingEligibilityEvaluation.IsEligible property must look at these rules to return true/false.
        var prevEval = new FundingEligibilityEvaluation { Rules = prevPassed ? new() : new() { new() { Passed = false } } };
        var currEval = new FundingEligibilityEvaluation { Rules = currPassed ? new() : new() { new() { Passed = false } } };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>())).Returns(currEval);

        _eligibilityMock.Setup(s => s.CompareFundingEvaluations(It.IsAny<FundingEligibilityEvaluation>(), It.IsAny<FundingEligibilityEvaluation>()))
            .Returns(new FundingEligibilityComparison
            {
                PreviousEvaluation = prevEval,
                CurrentEvaluation = currEval
            });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true, KeyFieldsChanged = hasKeyChanges });

        var expectedStatusId = GetGuidFromSettings(expectedStatusProp);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);
    }


    [Theory]
    // [Key Changes] | [Start Status Property] | [Expected Note Keyword]
    [InlineData(true, "OnHoldStatusId", "Major")]
    [InlineData(false, "OnHoldStatusId", "Minor")]
    [InlineData(true, "DecisionRequiredStatusId", "Major")]
    [InlineData(false, "DecisionRequiredStatusId", "Minor")]
    public void Process_ExistingInReview_MaintainsStatus(
    bool hasKeyChanges,
    string startingStatusProp,
    string expectedNoteWord)
    {
        // Arrange
        var statusId = GetGuidFromSettings(startingStatusProp);

        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = statusId,
            LifecycleStageId = _settings.ChangedLifecycleStageId,
            Version = 1,
            QualificationId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" }
        };

        // Mocks for a "No Eligibility Change" scenario
        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(new FundingEligibilityEvaluation { Rules = new() });

        _eligibilityMock.Setup(s => s.CompareFundingEvaluations(It.IsAny<FundingEligibilityEvaluation>(), It.IsAny<FundingEligibilityEvaluation>()))
            .Returns(new FundingEligibilityComparison
            {
                PreviousEvaluation = new(),
                CurrentEvaluation = new()
            });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true, KeyFieldsChanged = hasKeyChanges });

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, existingVersion.QualificationId, Guid.NewGuid(), false, false, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(statusId, result.NewVersion.ProcessStatusId);
        Assert.Contains(expectedNoteWord, result.Discussion.Notes);
    }
    [Fact]
    public void Process_UnknownStatus_DefaultsToDecisionRequired()
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = Guid.NewGuid(), // An ID not defined in your settings
            Qualification = new Qualification { Qan = "123" }
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>())).Returns(new FundingEligibilityEvaluation { Rules = new() });
        _eligibilityMock.Setup(s => s.CompareFundingEvaluations(It.IsAny<FundingEligibilityEvaluation>(), It.IsAny<FundingEligibilityEvaluation>()))
            .Returns(new FundingEligibilityComparison());

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false, _settings);

        // Assert
        // Safety check: Unknown status must trigger a review
        Assert.NotNull(result);
        Assert.Equal(_settings.DecisionRequiredStatusId, result.NewVersion.ProcessStatusId);
        Assert.Contains("Changed Qualification (Eligible) - Decision required", result.Discussion.Notes);
    }

    private Guid GetGuidFromSettings(string propertyName)
    {
        var property = _settings.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException(
                $"Property '{propertyName}' not found on {_settings.GetType().Name}.");

        var value = property.GetValue(_settings);

        return value switch
        {
            Guid g => g,
            null => throw new InvalidOperationException($"Property '{propertyName}' is null."),
            _ => throw new InvalidOperationException($"Property '{propertyName}' is not a Guid.")
        };
    }

}