using Polly;
using SFA.DAS.AODP.Infrastructure.Services;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using Shouldly;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QualificationProcessorTests
{
    private readonly Mock<IFundingEligibilityService> _eligibilityMock;
    private readonly Mock<IChangeDetectionService> _changeMock;
    private readonly Mock<IGuidProvider> _guidProviderMock;
    private readonly QualificationProcessor _processor;

    public QualificationProcessorTests()
    {
        _eligibilityMock = new Mock<IFundingEligibilityService>();
        _changeMock = new Mock<IChangeDetectionService>();
        _guidProviderMock = new Mock<IGuidProvider>();

        _processor = new QualificationProcessor(
            _eligibilityMock.Object,
            _changeMock.Object,
            _guidProviderMock.Object
        );
    }

    [Theory]
    [InlineData(true, false, true, ConflictTypes.None, TestDisplayName = "Eligible -> Decision Required")]   // Eligible -> Decision Required
    [InlineData(false, true, true, ConflictTypes.ActiveApplications, TestDisplayName = "Ineligible + Conflict -> Decision Required")]   // Ineligible + Conflict -> Decision Required
    [InlineData(false, false, false, ConflictTypes.None, TestDisplayName = "Ineligible + No Conflict -> No Action Required")] // Ineligible + No Conflict -> No Action Required
    public void Process_NewRecord_Paths(bool isEligible, bool hasActiveApps, bool expectDecisionRequired, string conflictType)
    {
        // Arrange
        var qualVersionId = Guid.NewGuid();
        var versionFieldChangeId = Guid.NewGuid();
        var qualificationDiscussion = Guid.NewGuid();

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

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(qualVersionId);
        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(VersionFieldChanges))).Returns(versionFieldChangeId);
        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationDiscussionHistory))).Returns(qualificationDiscussion);

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
            VersionFieldChanges = result.NewVersion.VersionFieldChanges,
            FundingEligibilityConflictType = conflictType,
        };

        // Assert
        static (string? note, Guid? ActionTypeId) GetDiscussionHistoryExpectedValues()
        {
            if (TestContext.Current.Test!.TestDisplayName.StartsWith("Eligible -> Decision Required"))
            {
                return ("decision required - new qualification", Guid.Parse("00000000-0000-0000-0000-000000000002"));
            }

            if (TestContext.Current.Test!.TestDisplayName.StartsWith("Ineligible + Conflict -> Decision Required"))
            {
                return ("decision required - new qualification - active applications", Guid.Parse("00000000-0000-0000-0000-000000000002"));
            }

            if (TestContext.Current.Test!.TestDisplayName.StartsWith("Ineligible + No Conflict -> No Action Required"))
            {
                return ("no action required - new qualification", Guid.Parse("00000000-0000-0000-0000-000000000001"));
            }

            return (null, null);
        }

        Assert.NotNull(result);
        var discussion = new QualificationDiscussionHistory
        {
            ActionTypeId = GetDiscussionHistoryExpectedValues().ActionTypeId!.Value,
            Notes = GetDiscussionHistoryExpectedValues().note,
            Id = qualificationDiscussion,
            QualificationId = result.NewVersion.QualificationId,
            UserDisplayName = "OFQUAL Import"
        };

        var expectedVersionFieldChanges = new VersionFieldChanges { Id = versionFieldChangeId, ChangedFieldNames = null, QualificationVersionNumber = 1 };
        var expectedResult = new QualificationProcessor.QualificationProcessorResult(result.NewVersion, discussion, expectedVersionFieldChanges, null);
        
        // Ugly hack but works for now.
        expectedResult.Discussion.Timestamp = result.Discussion.Timestamp;
        result.ShouldBeEquivalentTo(expectedResult);
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
        var existingQualificationVersionId = Guid.NewGuid();
        var newQualificationVersionId = Guid.NewGuid();

        var existingVersion = new QualificationVersions
        {
            Id = existingQualificationVersionId,
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

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(newQualificationVersionId);

        var expectedStatusId = expectDecisionRequired
            ? ProcessStatusLookup.DecisionRequired.Id
            : ProcessStatusLookup.NoActionRequired.Id;

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), hasApps, hasFunding);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(expectedStatusId);

        result.FundingTracker.ShouldBeEquivalentTo(new QualificationFundingTracker
        {
            NewVersionId = newQualificationVersionId,
            OldVersionId = existingQualificationVersionId
        });

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
        var existingQualificationVersionId = Guid.NewGuid();
        var newQualificationVersionId = Guid.NewGuid();
        var existingVersion = new QualificationVersions
        {
            Id = existingQualificationVersionId,
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

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(newQualificationVersionId);

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
        result.FundingTracker.ShouldBeEquivalentTo(new QualificationFundingTracker
        {
            NewVersionId = newQualificationVersionId,
            OldVersionId = existingQualificationVersionId
        });
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
        var existingQualificationVersionId = Guid.NewGuid();
        var newQualificationVersionId = Guid.NewGuid();
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
            Id = existingQualificationVersionId,
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

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(newQualificationVersionId);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, existingVersion.QualificationId, Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(statusId);
        result.Discussion.Notes!.ShouldContain(expectedNoteWord);
        result.FundingTracker.ShouldBeEquivalentTo(new QualificationFundingTracker
        {
            NewVersionId = newQualificationVersionId,
            OldVersionId = existingQualificationVersionId
        });
    }

    [Fact]
    public void Process_UnknownStatus_NoReviewRequired_DefaultsToNoActionRequired()
    {
        // Arrange
        var existingQualificationVersionId = Guid.NewGuid();
        var newQualificationVersionId = Guid.NewGuid();
        var existingVersion = new QualificationVersions
        {
            Id = existingQualificationVersionId,
            ProcessStatusId = Guid.NewGuid(),
            Qualification = new Qualification { Qan = "123" },
            EligibleForFunding = true
        };

        _eligibilityMock.Setup(s => s.EvaluateFundingEligibilityRules(It.IsAny<QualificationDTO>()))
            .Returns(new FundingEligibilityEvaluation { Rules = [] });

        _changeMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), existingVersion))
            .Returns(new DetectionResults { ChangesPresent = true });

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(newQualificationVersionId);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(ProcessStatusLookup.NoActionRequired.Id);
        result.Discussion.Notes!.ShouldContain("no action required - changed qualification");
        result.FundingTracker.ShouldBeEquivalentTo(new QualificationFundingTracker
        {
            NewVersionId = newQualificationVersionId,
            OldVersionId = existingQualificationVersionId
        });
    }

    [Fact]
    public void Process_UnknownStatus_ReviewRequired_EligibilityChanged_DecisionRequiredAndChanged()
    {
        // Arrange
        var existingQualificationVersionId = Guid.NewGuid();
        var newQualificationVersionId = Guid.NewGuid();
        var existingVersion = new QualificationVersions
        {
            Id = existingQualificationVersionId,
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

        _guidProviderMock.Setup(o => o.NewGuidFor(nameof(QualificationVersions))).Returns(newQualificationVersionId);

        // Act
        var result = _processor.Process(new QualificationDTO(), existingVersion, Guid.NewGuid(), Guid.NewGuid(), false, false);

        // Assert
        result.ShouldNotBeNull();
        result.NewVersion.ProcessStatusId.ShouldBe(ProcessStatusLookup.DecisionRequired.Id);
        result.Discussion.Notes!.ShouldContain("decision required - changed qualification");
        result.FundingTracker.ShouldBeEquivalentTo(new QualificationFundingTracker
        {
            NewVersionId = newQualificationVersionId,
            OldVersionId = existingQualificationVersionId
        });
    }
}