using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class QualificationProcessor : IQualificationProcessor
    {
        private sealed record OutcomeContext(
            Guid? ExistingStatusId,
            Guid? ExistingStageId,
            bool IsNew,
            bool IsEligible,
            bool HasKeyChanges,
            bool EligibilityChanged,
            bool HasActiveApps,
            bool HasActiveFunding);

        private readonly IFundingEligibilityService _fundingService;
        private readonly IChangeDetectionService _changeService;

        public QualificationProcessor(
            IFundingEligibilityService fundingService,
            IChangeDetectionService changeService)
        {
            _fundingService = fundingService;
            _changeService = changeService;
        }

        public QualificationProcessorResult? Process(
            QualificationDTO importRecord,
            QualificationVersions? existingVersion,
            Guid qualificationId,
            Guid organisationId,
            bool hasActiveApps,
            bool hasActiveFunding,
            QualificationProcessorSettings settings)
        {
            var incomingEval = _fundingService.EvaluateFundingEligibilityRules(importRecord);
            DetectionResults? changes = null;
            bool eligibilityChanged = false;
            bool hasKeyChanges = false;

            if (existingVersion != null)
            {
                var previousVersionDTO = MapToQualificationDto(existingVersion);
                var previousEval = _fundingService.EvaluateFundingEligibilityRules(previousVersionDTO);
                var comparison = _fundingService.CompareFundingEvaluations(previousEval, incomingEval);

                changes = _changeService.DetectChanges(importRecord, existingVersion);

                if (!changes.Value.ChangesPresent && !comparison.EligibilityChanged)
                {
                    return null;
                }

                eligibilityChanged = comparison.EligibilityChanged;
                hasKeyChanges = changes.Value.KeyFieldsChanged;
            }
                var context = new OutcomeContext(
                    existingVersion?.ProcessStatusId,
                    existingVersion?.LifecycleStageId,
                    existingVersion == null,
                    incomingEval.IsEligible,
                    hasKeyChanges,
                    eligibilityChanged,
                    hasActiveApps,
                    hasActiveFunding);

            var outcome = DetermineOutcome(context,settings);

            return BuildResult(
                importRecord,
                existingVersion,
                qualificationId,
                organisationId,
                outcome,
                changes,
                incomingEval
            );
        }

        private static QualificationProcessorOutcome DetermineOutcome(
            OutcomeContext context,
            QualificationProcessorSettings settings)
        {
            var isApprovedOrRejected = 
                context.ExistingStatusId == settings.ApprovedStatusId ||
                context.ExistingStatusId == settings.RejectedStatusId;

            var isInReview =
                context.ExistingStatusId == settings.OnHoldStatusId ||
                context.ExistingStatusId == settings.DecisionRequiredStatusId;

            var hasUsageConflict = context.HasActiveApps || context.HasActiveFunding;
            var requiresRereview = context.HasKeyChanges || context.EligibilityChanged;

            //New and eligible
            if (context.IsNew && context.IsEligible)
            {
                return new QualificationProcessorOutcome(
                    StatusId: settings.DecisionRequiredStatusId,
                    StageId: settings.NewLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: "New Qualification (Eligible) - Decision Required",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: true,
                    HasFunding: false);
            }

            //New and not eligible
            if (context.IsNew )
            {
                var hasConflict = context.HasActiveApps;

                var statusId = hasConflict
                    ? settings.DecisionRequiredStatusId
                    : settings.NoActionRequiredStatusId;

                var actionId = hasConflict
                    ? settings.ActionTypeDecisionId
                    : settings.ActionTypeNoActionId;

                var baseNote = hasConflict
                    ? "New Qualification (Ineligible) - Decision required - Qualification has Active Applications"
                    : "New Qualification (Ineligible) - No action required";

                return new QualificationProcessorOutcome(
                    StatusId: statusId,
                    StageId: settings.NewLifecycleStageId,
                    ActionId: actionId,
                    BaseNote: baseNote,
                    IncludeFieldChanges: hasConflict,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: hasConflict,
                    HasFunding: false);
            }

            //Changed and not eligible (conflict or eligibility changed)
            if (!context.IsEligible && (hasUsageConflict || context.EligibilityChanged))
            {
                return new QualificationProcessorOutcome(
                    StatusId: settings.DecisionRequiredStatusId,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: "Changed Qualification (Ineligible) - Decision required - Conflict or Eligibility Change",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: true,
                    HasFunding: context.HasActiveFunding);
            }

            //Changed and not eligible (no conflict or eligibility change)
            if (!context.IsEligible)
            {
                return new QualificationProcessorOutcome(
                    StatusId: settings.NoActionRequiredStatusId,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeNoActionId,
                    BaseNote: "Changed Qualification (Ineligible) - No action required.",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: false,
                    HasFunding: false);
            }

            //Changed and eligible (Approved or Rejected + Key Changes or Eligibility Change)
            if (isApprovedOrRejected && requiresRereview)
            {
                return new QualificationProcessorOutcome(
                    StatusId: settings.DecisionRequiredStatusId,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: "Changed Qualification (Eligible) - Decision required - Major change",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: true,
                    HasFunding: context.HasActiveFunding);
            }

            //Changed and eligible (Approved or Rejected + Minor Change)
            if (isApprovedOrRejected)
            {
                return new QualificationProcessorOutcome(
                    StatusId: context.ExistingStatusId!.Value,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeNoActionId,
                    BaseNote: "Changed Qualification (Eligible) - No action required - Minor change",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: false,
                    HasFunding: context.HasActiveFunding);
            }

            //Changed and eligible (On Hold or Decision required)
            if (isInReview)
            {
                return new QualificationProcessorOutcome(
                    StatusId: context.ExistingStatusId!.Value,
                    StageId: context.ExistingStageId ?? settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: $"Changed Qualification (Eligible) - No status change - ({(context.HasKeyChanges ? "Major" : "Minor")} change)",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: requiresRereview,
                    HasFunding: context.HasActiveFunding);
            }

            //Changed and eligible (unknown status)
            return new QualificationProcessorOutcome(
                StatusId: settings.DecisionRequiredStatusId,
                StageId: settings.ChangedLifecycleStageId,
                ActionId: settings.ActionTypeDecisionId,
                BaseNote: "Changed Qualification (Eligible) - Decision required",
                IncludeFieldChanges: true,
                IncludeEligibilityReasons: true,
                ReviewRequired: true,
                HasFunding: context.HasActiveFunding);
        }

        private QualificationProcessorResult BuildResult(
            QualificationDTO import,
            QualificationVersions? existing,
            Guid qId,
            Guid oId,
            QualificationProcessorOutcome outcome,
            DetectionResults? changes,
            FundingEligibilityEvaluation eval)
        {
            var versionNumber = (existing?.Version ?? 0) + 1;

            var fieldChange = new VersionFieldChanges
            {
                Id = Guid.NewGuid(),
                QualificationVersionNumber = versionNumber,
                ChangedFieldNames = outcome.IncludeFieldChanges ? changes?.ChangedFieldsCsv : null
            };

            var noteLines = new List<string> { outcome.BaseNote };
            if (outcome.IncludeEligibilityReasons && !string.IsNullOrEmpty(eval.GetFailedFieldsCsv()))
            {
                noteLines.Add($"Ineligible: {eval.GetFailedFieldsCsv()}");
            }
            if (outcome.IncludeFieldChanges && !string.IsNullOrEmpty(changes?.ChangedFieldsCsv))
            {
                noteLines.Add($"Changes: {changes.Value.ChangedFieldsCsv}");
            }

            var discussion = new QualificationDiscussionHistory
            {
                Id = Guid.NewGuid(),
                QualificationId = qId,
                ActionTypeId = outcome.ActionId,
                Notes = string.Join(" | ", noteLines),
                Timestamp = DateTime.Now,
                UserDisplayName = "OFQUAL Import"
            };

            var newVersion = CreateQualificationVersion(qId, oId, outcome.StageId, outcome.StatusId, import, fieldChange, eval.IsEligible, versionNumber, eval.GetFailedFieldsCsv());

            QualificationFundingTracker? tracker = null;
            if (existing != null && outcome.HasFunding)
            {
                tracker = new QualificationFundingTracker { OldVersionId = existing.Id, NewVersionId = newVersion.Id };
            }

            return new QualificationProcessorResult(newVersion, discussion, fieldChange, tracker);
        }

        private static QualificationDTO MapToQualificationDto(QualificationVersions version)
        {
            return new QualificationDTO
            {
                QualificationNumberNoObliques = version.Qualification?.Qan,
                Title = version.Qualification?.QualificationName ?? string.Empty,
                Type = version.Type,
                OfferedInEngland = version.OfferedInEngland,
                IntentionToSeekFundingInEngland = version.IntentionToSeekFundingInEngland,
                Glh = version.Glh,
                Tqt = version.Tqt
            };
        }

        private static QualificationVersions CreateQualificationVersion(Guid qualificationId, Guid organisationId, Guid lifecycleStageId,
            Guid processStatusId, QualificationDTO qualificationData, VersionFieldChanges versionFieldChange, bool eligibleForFunding,
            int? version, string eligibilityChangeReason)
        {

            return new QualificationVersions
            {
                Id = Guid.NewGuid(),
                QualificationId = qualificationId,
                VersionFieldChangesId = versionFieldChange.Id,
                ProcessStatusId = processStatusId,
                AdditionalKeyChangesReceivedFlag = 0,
                LifecycleStageId = lifecycleStageId,
                AwardingOrganisationId = organisationId,
                Status = qualificationData.Status,
                Type = qualificationData.Type,
                Ssa = qualificationData.Ssa,
                Level = qualificationData.Level,
                SubLevel = qualificationData.SubLevel,
                EqfLevel = qualificationData.EqfLevel,
                GradingType = qualificationData.GradingType,
                GradingScale = qualificationData.GradingScale,
                TotalCredits = qualificationData.TotalCredits,
                Tqt = qualificationData.Tqt,
                Glh = qualificationData.Glh,
                MinimumGlh = qualificationData.MinimumGlh,
                MaximumGlh = qualificationData.MaximumGlh,
                RegulationStartDate = qualificationData.RegulationStartDate,
                OperationalStartDate = qualificationData.OperationalStartDate,
                OperationalEndDate = qualificationData.OperationalEndDate,
                CertificationEndDate = qualificationData.CertificationEndDate,
                ReviewDate = qualificationData.ReviewDate,
                OfferedInEngland = qualificationData.OfferedInEngland,
                OfferedInNi = qualificationData.OfferedInNorthernIreland,
                OfferedInternationally = qualificationData.OfferedInternationally,
                Specialism = qualificationData.Specialism,
                Pathways = qualificationData.Pathways,
                ApprovedForDelFundedProgramme = qualificationData.ApprovedForDelfundedProgramme,
                LinkToSpecification = qualificationData.LinkToSpecification,
                ApprenticeshipStandardReferenceNumber = qualificationData.ApprenticeshipStandardReferenceNumber,
                ApprenticeshipStandardTitle = qualificationData.ApprenticeshipStandardTitle,
                RegulatedByNorthernIreland = qualificationData.RegulatedByNorthernIreland,
                NiDiscountCode = qualificationData.NiDiscountCode,
                GceSizeEquivelence = qualificationData.GceSizeEquivalence,
                GcseSizeEquivelence = qualificationData.GcseSizeEquivalence,
                EntitlementFrameworkDesign = qualificationData.EntitlementFrameworkDesignation,
                LastUpdatedDate = qualificationData.LastUpdatedDate,
                UiLastUpdatedDate = qualificationData.UiLastUpdatedDate,
                InsertedDate = qualificationData.InsertedDate,
                Version = version,
                AppearsOnPublicRegister = qualificationData.AppearsOnPublicRegister,
                LevelId = qualificationData.LevelId,
                TypeId = qualificationData.TypeId,
                SsaId = qualificationData.SsaId,
                GradingTypeId = qualificationData.GradingTypeId,
                GradingScaleId = qualificationData.GradingScaleId,
                PreSixteen = qualificationData.PreSixteen,
                SixteenToEighteen = qualificationData.SixteenToEighteen,
                EighteenPlus = qualificationData.EighteenPlus,
                NineteenPlus = qualificationData.NineteenPlus,
                ImportStatus = qualificationData.ImportStatus,
                VersionFieldChanges = versionFieldChange,
                InsertedTimestamp = DateTime.Now,
                EligibleForFunding = eligibleForFunding,
                Name = qualificationData.Title,
                IntentionToSeekFundingInEngland = qualificationData.IntentionToSeekFundingInEngland,
                EligibleForFundingChangeReason = eligibilityChangeReason,
            };
        }
    }

    
}
