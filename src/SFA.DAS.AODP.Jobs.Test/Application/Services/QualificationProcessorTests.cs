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
    [InlineData(false, true, true)]   // Ineligible + Conflict -> Decision Required
    [InlineData(false, false, false)] // Ineligible + No Conflict -> No Action Required
    public void Process_NewRecord_Paths(bool isEligible, bool hasActiveApps, bool expectDecisionRequired)
    {
        // Arrange
        var qualVersionId = Guid.NewGuid();
        var dto = new QualificationDTO
        {
            Id = qualVersionId,
            QualificationNumber = "QUAL/1234",
            QualificationNumberNoObliques = "12345",
            Title = "New Qual",
            Status = "Active",
            OrganisationName = "Test Org",
            OrganisationAcronym = "TO",
            OrganisationRecognitionNumber = "ORG123",
            Type = "TypeA",
            Ssa = "SSA1",
            Level = "3",
            SubLevel = "A",
            EqfLevel = "4",

            GradingType = "Pass/Fail",
            GradingScale = "A-F",

            TotalCredits = 120,
            Tqt = 600,
            Glh = 300,
            MinimumGlh = 200,
            MaximumGlh = 400,

            RegulationStartDate = new DateTime(2020, 1, 1),
            OperationalStartDate = new DateTime(2020, 6, 1),
            OperationalEndDate = new DateTime(2025, 6, 1),
            CertificationEndDate = new DateTime(2026, 6, 1),
            ReviewDate = new DateTime(2024, 1, 1),

            OfferedInEngland = true,
            OfferedInNorthernIreland = true,
            OfferedInternationally = true,

            Specialism = "Engineering",
            Pathways = "Pathway1",

            AssessmentMethods = new[] { "Exam", "Coursework" },

            ApprovedForDelfundedProgramme = "Yes",
            LinkToSpecification = "http://spec-link",

            ApprenticeshipStandardReferenceNumber = "AST123",
            ApprenticeshipStandardTitle = "Apprenticeship Title",

            RegulatedByNorthernIreland = true,
            NiDiscountCode = "NIDC123",

            GceSizeEquivalence = "1 A-Level",
            GcseSizeEquivalence = "3 GCSEs",
            EntitlementFrameworkDesignation = "Designation1",

            LastUpdatedDate = new DateTime(2024, 5, 1),
            UiLastUpdatedDate = new DateTime(2024, 5, 2),
            InsertedDate = new DateTime(2024, 1, 1),

            Version = 1,
            AppearsOnPublicRegister = true,

            OrganisationId = 999,
            LevelId = 3,
            TypeId = 10,
            SsaId = 5,
            GradingTypeId = 2,
            GradingScaleId = 3,

            PreSixteen = false,
            SixteenToEighteen = true,
            EighteenPlus = true,
            NineteenPlus = true,

            ImportStatus = "Imported",
            ChangedFields = "All",

            IntentionToSeekFundingInEngland = true
        };

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
                    : [new FundingEligibilityRuleResult("rule", false, [])]
            });

        // Act
        var result = _processor.Process(dto, null, qualId, orgId, hasActiveApps, false);

        // Build expected AFTER Act so we can use result values where needed
        var expected = new QualificationVersions
        {
            Id = qualVersionId,
            QualificationId = qualId,
            ProcessStatusId = expectedStatusId,
            LifecycleStageId = expectDecisionRequired
                ? LifecycleStageLookup.New.Id
                : LifecycleStageLookup.Completed.Id,

            AdditionalKeyChangesReceivedFlag = 0,
            AwardingOrganisationId = orgId,

            AssessmentMethods = "Exam, Coursework",

            Status = dto.Status,
            Type = dto.Type,
            Ssa = dto.Ssa,
            Level = dto.Level,
            SubLevel = dto.SubLevel,
            EqfLevel = dto.EqfLevel,

            GradingType = dto.GradingType,
            GradingScale = dto.GradingScale,

            TotalCredits = dto.TotalCredits,
            Tqt = dto.Tqt,
            Glh = dto.Glh,
            MinimumGlh = dto.MinimumGlh,
            MaximumGlh = dto.MaximumGlh,

            RegulationStartDate = dto.RegulationStartDate,
            OperationalStartDate = dto.OperationalStartDate,
            OperationalEndDate = dto.OperationalEndDate,
            CertificationEndDate = dto.CertificationEndDate,
            ReviewDate = dto.ReviewDate,

            OfferedInEngland = dto.OfferedInEngland,
            OfferedInNi = dto.OfferedInNorthernIreland,
            OfferedInternationally = dto.OfferedInternationally,

            Specialism = dto.Specialism,
            Pathways = dto.Pathways,

            ApprovedForDelFundedProgramme = dto.ApprovedForDelfundedProgramme,
            LinkToSpecification = dto.LinkToSpecification,

            ApprenticeshipStandardReferenceNumber = dto.ApprenticeshipStandardReferenceNumber,
            ApprenticeshipStandardTitle = dto.ApprenticeshipStandardTitle,

            RegulatedByNorthernIreland = dto.RegulatedByNorthernIreland,
            NiDiscountCode = dto.NiDiscountCode,

            GceSizeEquivelence = dto.GceSizeEquivalence,
            GcseSizeEquivelence = dto.GcseSizeEquivalence,
            EntitlementFrameworkDesign = dto.EntitlementFrameworkDesignation,

            LastUpdatedDate = dto.LastUpdatedDate,
            UiLastUpdatedDate = dto.UiLastUpdatedDate,
            InsertedDate = dto.InsertedDate,

            Version = 1,
            AppearsOnPublicRegister = dto.AppearsOnPublicRegister,

            LevelId = dto.LevelId,
            TypeId = dto.TypeId,
            SsaId = dto.SsaId,
            GradingTypeId = dto.GradingTypeId,
            GradingScaleId = dto.GradingScaleId,

            PreSixteen = dto.PreSixteen,
            SixteenToEighteen = dto.SixteenToEighteen,
            EighteenPlus = dto.EighteenPlus,
            NineteenPlus = dto.NineteenPlus,

            ImportStatus = dto.ImportStatus,

            InsertedTimestamp = result.NewVersion.InsertedTimestamp,

            EligibleForFunding = isEligible,
            Name = dto.Title,
            IntentionToSeekFundingInEngland = dto.IntentionToSeekFundingInEngland,
            VersionFieldChanges = result.NewVersion.VersionFieldChanges
        };

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.NewVersion.Version);
        Assert.Equal(qualId, result.NewVersion.QualificationId);
        Assert.Equal(expectedStatusId, result.NewVersion.ProcessStatusId);

        result.NewVersion.Id = expected.Id;
        result.NewVersion.VersionFieldChangesId = expected.VersionFieldChangesId;
        result.NewVersion.ShouldBeEquivalentTo(expected);
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
    public void Process_UnknownStatus_NoReviewRequired_DefaultsToNoActionRequired()
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" },
            EligibleForFunding = true
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

    [Fact]
    public void Process_UnknownStatus_ReviewRequired_EligibilityChanged_DecisionRequiredAndChanged()
    {
        // Arrange
        var existingVersion = new QualificationVersions
        {
            ProcessStatusId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" },
            EligibleForFunding = false
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(new FundingEligibilityEvaluation { Rules =
                [new("Rule name", true, new List<string> { "IntentionToSeekFundingInEngland" })]
            });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(ProcessStatusLookup.DecisionRequired.Id);
        result.Discussion.Notes!.ShouldContain("decision required - changed qualification");
    }
}