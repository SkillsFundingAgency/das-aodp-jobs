using DocumentFormat.OpenXml.Presentation;
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
    [InlineData(true, false, "DecisionRequiredStatusId")]   // Eligible
    [InlineData(false, true, "DecisionRequiredStatusId")]   // Ineligible + Conflict (Active Apps)
    [InlineData(false, false, "NoActionRequiredStatusId")]  // Ineligible + No Conflict
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
    // Eligibility Flipped? | Has Apps? | Has Funding? | Expected Status 
    [InlineData(true, false, false, "NoActionRequiredStatusId")]  // Flip to false + no conflict
    [InlineData(false, true, false, "DecisionRequiredStatusId")]  // App Conflict
    [InlineData(false, false, true, "DecisionRequiredStatusId")]  // Funding Conflict
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
            // If eligibilityChanged is true, the PREVIOUS state must have been 'true'
            EligibleForFunding = eligibilityChanged ? true : false,
            ProcessStatusId = _settings.NoActionRequiredStatusId,
            Qualification = new Qualification { Qan = "123" }
        };

        // Current state is ALWAYS ineligible for this test method
        var currentEval = new FundingEligibilityEvaluation
        {
            Rules = new List<FundingEligibilityRuleResult> { new() { Passed = false, Fields = { "Glh" } } }
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(currentEval);

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        var expectedStatusId = GetGuidFromSettings(expectedStatusProp);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), hasApps, hasFunding, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);

        if (expectedStatusProp == "DecisionRequiredStatusId")
        {
            Assert.True(result.FieldChange.ChangedFieldNames != null);
        }
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
            EligibleForFunding = prevPassed,
            Qualification = new Qualification { Qan = "123" }
        };

        var currEval = new FundingEligibilityEvaluation
        {
            Rules = currPassed
            ? new List<FundingEligibilityRuleResult>()
            : new List<FundingEligibilityRuleResult> { new() { Passed = false, Fields = { "TestField" } } }
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>())).Returns(currEval);

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