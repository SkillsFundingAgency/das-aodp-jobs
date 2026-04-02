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
        private sealed record CreateQualificationVersionRequest(
            Guid QualificationId,
            Guid OrganisationId,
            Guid LifecycleStageId,
            Guid ProcessStatusId,
            QualificationDTO QualificationData,
            VersionFieldChanges VersionFieldChange,
            bool EligibleForFunding,
            int? Version,
            string IneligibleForFundingFieldNames);
        public record QualificationProcessorResult(
            QualificationVersions NewVersion,
            QualificationDiscussionHistory Discussion,
            VersionFieldChanges FieldChange,
            QualificationFundingTracker? FundingTracker = null,
            bool TitleChanged = false
        );
        public record QualificationProcessorOutcome(
            Guid StatusId,
            Guid StageId,
            Guid ActionId,
            string BaseNote,
            bool ReviewRequired,
            bool IncludeFieldChanges,
            bool IncludeEligibilityReasons,
            bool HasFunding
        );


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
            bool eligibilityChanged = existingVersion?.EligibleForFunding != incomingEval.IsEligible;

            DetectionResults? changes = null;
            bool hasKeyChanges = false;

            if (existingVersion != null)
            {
                changes = _changeService.DetectChanges(importRecord, existingVersion);

                if (!changes.Value.ChangesPresent && !eligibilityChanged)
                {
                    return null;
                }

                if (eligibilityChanged)
                { 
                    changes.Value.ChangedFields.Add(nameof(QualificationVersions.EligibleForFunding));
                }

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
            if (context.IsNew)
                return DetermineNewQualificationOutcome(context, settings);

            return context.IsEligible
                ? DetermineEligibleChangeOutcome(context, settings)
                : DetermineIneligibleChangeOutcome(context, settings);
        }

        private static QualificationProcessorOutcome DetermineEligibleChangeOutcome(
            OutcomeContext context,
            QualificationProcessorSettings settings)
        {
            var isApprovedOrRejected = context.ExistingStatusId == settings.ApprovedStatusId ||
                                       context.ExistingStatusId == settings.RejectedStatusId;

            var isInReview = context.ExistingStatusId == settings.OnHoldStatusId ||
                     context.ExistingStatusId == settings.DecisionRequiredStatusId;

            var requiresRereview = context.HasKeyChanges || context.EligibilityChanged;

            if (isApprovedOrRejected)
            {
                return new QualificationProcessorOutcome(
                    StatusId: requiresRereview ? settings.DecisionRequiredStatusId : context.ExistingStatusId!.Value,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: requiresRereview ? settings.ActionTypeDecisionId : settings.ActionTypeNoActionId,
                    BaseNote: requiresRereview ? "Eligible - Major change" : "Eligible - Minor change",
                    IncludeFieldChanges: requiresRereview,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: requiresRereview,
                    HasFunding: context.HasActiveFunding);
            }

            if (isInReview)
            {
                return new QualificationProcessorOutcome(
                    StatusId: context.ExistingStatusId!.Value,
                    StageId: context.ExistingStageId ?? settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: $"Changed Qualification (Eligible) - No status change - ({(context.HasKeyChanges ? "Major" : "Minor")} change)",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: requiresRereview,
                    HasFunding: context.HasActiveFunding);
            }

            return new QualificationProcessorOutcome(
                StatusId: settings.DecisionRequiredStatusId,
                StageId: settings.ChangedLifecycleStageId,
                ActionId: settings.ActionTypeDecisionId,
                BaseNote: "Changed Qualification (Eligible) - Decision required",
                IncludeFieldChanges: true,
                IncludeEligibilityReasons: false,
                ReviewRequired: true,
                HasFunding: context.HasActiveFunding);
        }

        private static QualificationProcessorOutcome DetermineIneligibleChangeOutcome(
            OutcomeContext context,
            QualificationProcessorSettings settings)
        {
            var hasUsageConflict = context.HasActiveApps || context.HasActiveFunding;
            var needsDecision = hasUsageConflict;

            var statusId = needsDecision ? settings.DecisionRequiredStatusId : settings.NoActionRequiredStatusId;
            var actionId = needsDecision ? settings.ActionTypeDecisionId : settings.ActionTypeNoActionId;
            var note = needsDecision
                ? "Changed Qualification (Ineligible) - Decision required - Conflict or Eligibility Change"
                : "Changed Qualification (Ineligible) - No action required.";

            return new QualificationProcessorOutcome(
                StatusId: statusId,
                StageId: settings.ChangedLifecycleStageId,
                ActionId: actionId,
                BaseNote: note,
                IncludeFieldChanges: needsDecision,
                IncludeEligibilityReasons: true,
                ReviewRequired: needsDecision,
                HasFunding: context.HasActiveFunding);
        }

        private static QualificationProcessorOutcome DetermineNewQualificationOutcome(OutcomeContext context, QualificationProcessorSettings settings)
        {
            if (context.IsEligible)
            {
                return new QualificationProcessorOutcome(
                    StatusId: settings.DecisionRequiredStatusId,
                    StageId: settings.NewLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: "New Qualification (Eligible) - Decision Required",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: true,
                    HasFunding: false);
            }

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
                IncludeFieldChanges: false,
                IncludeEligibilityReasons: true,
                ReviewRequired: hasConflict,
                HasFunding: false);
        }

        private static QualificationProcessorResult BuildResult(
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

            var newVersion = CreateQualificationVersion(
                new CreateQualificationVersionRequest(
                    QualificationId: qId,
                    OrganisationId: oId,
                    LifecycleStageId: outcome.StageId,
                    ProcessStatusId: outcome.StatusId,
                    QualificationData: import,
                    VersionFieldChange: fieldChange,
                    EligibleForFunding: eval.IsEligible,
                    Version: versionNumber,
                    IneligibleForFundingFieldNames: eval.GetFailedFieldsCsv()));

            QualificationFundingTracker? tracker = null;
            if (existing != null && outcome.HasFunding)
            {
                tracker = new QualificationFundingTracker { OldVersionId = existing.Id, NewVersionId = newVersion.Id };
            }

            return new QualificationProcessorResult(newVersion, discussion, fieldChange, tracker);
        }

        private static QualificationVersions CreateQualificationVersion(CreateQualificationVersionRequest request)
        {
            var qualificationData = request.QualificationData;

            return new QualificationVersions
            {
                Id = Guid.NewGuid(),
                QualificationId = request.QualificationId,
                VersionFieldChangesId = request.VersionFieldChange.Id,
                ProcessStatusId = request.ProcessStatusId,
                AdditionalKeyChangesReceivedFlag = 0,
                LifecycleStageId = request.LifecycleStageId,
                AwardingOrganisationId = request.OrganisationId,
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
                Version = request.Version,
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
                VersionFieldChanges = request.VersionFieldChange,
                InsertedTimestamp = DateTime.Now,
                EligibleForFunding = request.EligibleForFunding,
                Name = qualificationData.Title,
                IntentionToSeekFundingInEngland = qualificationData.IntentionToSeekFundingInEngland,
                EligibleForFundingChangeReason = request.IneligibleForFundingFieldNames,
            };
        }
    }

    
}
