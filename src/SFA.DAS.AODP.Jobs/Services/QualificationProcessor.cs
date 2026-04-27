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
            bool hasAnyChanges,
            bool HasKeyChanges,
            bool EligibilityChanged,
            bool HasApplicationsInProgress,
            bool HasFundingWhichHasNotEnded);
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
            bool HasFundingWhichHasNotEnded
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
            bool hasApplicationsInProgress,
            bool hasFundingWhichHasNotEnded)
        {
            var incomingEval = _fundingService.EvaluateFundingEligibilityRules(importRecord);
            bool eligibilityChanged = existingVersion?.EligibleForFunding != incomingEval.IsEligible;

            DetectionResults? changes = null;
            bool hasChanges = false;
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

                hasChanges = changes.Value.ChangesPresent; 
                hasKeyChanges = changes.Value.KeyFieldsChanged;
            }

            var context = new OutcomeContext(
                existingVersion?.ProcessStatusId,
                existingVersion?.LifecycleStageId,
                existingVersion == null,
                incomingEval.IsEligible,
                hasChanges,
                hasKeyChanges,
                eligibilityChanged,
                hasApplicationsInProgress,
                hasFundingWhichHasNotEnded);

            var outcome = DetermineOutcome(context);

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
            OutcomeContext context)
        {
            if (context.IsNew)
                return DetermineNewQualificationOutcome(context);

            return context.IsEligible
                ? DetermineEligibleChangeOutcome(context)
                : DetermineIneligibleChangeOutcome(context);
        }

        private static QualificationProcessorOutcome DetermineEligibleChangeOutcome(
            OutcomeContext context)
        {
            var isApprovedOrRejected = context.ExistingStatusId == ProcessStatusLookup.Approved.Id ||
                                       context.ExistingStatusId == ProcessStatusLookup.Rejected.Id;

            var isInReview = context.ExistingStatusId == ProcessStatusLookup.OnHold.Id ||
                     context.ExistingStatusId == ProcessStatusLookup.DecisionRequired.Id;

            var requiresRereview = context.HasKeyChanges || context.EligibilityChanged;


            if (isApprovedOrRejected)
            {
                var statusId = requiresRereview ? ProcessStatusLookup.DecisionRequired.Id : context.ExistingStatusId!.Value;
                var actionId = requiresRereview ? ActionTypeLookup.ActionRequired.Id : ActionTypeLookup.NoActionRequired.Id;
                var stageId = requiresRereview ? LifecycleStageLookup.Changed.Id : LifecycleStageLookup.Completed.Id;

                return new QualificationProcessorOutcome(
                    StatusId: statusId,
                    StageId: stageId,
                    ActionId: actionId,
                    BaseNote: requiresRereview ? "decision required - changed qualification" : "no action required - changed qualification",
                    IncludeFieldChanges: requiresRereview,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: requiresRereview,
                    HasFundingWhichHasNotEnded: context.HasFundingWhichHasNotEnded);
            }

            if (isInReview)
            {
                return new QualificationProcessorOutcome(
                    StatusId: context.ExistingStatusId!.Value,
                    StageId: context.ExistingStageId ?? LifecycleStageLookup.Changed.Id,
                    ActionId: ActionTypeLookup.ActionRequired.Id,
                    BaseNote: $"no status change - changed qualification - ({(context.HasKeyChanges ? "major" : "minor")} change)",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: requiresRereview,
                    HasFundingWhichHasNotEnded: context.HasFundingWhichHasNotEnded);
            }

            return new QualificationProcessorOutcome(
                StatusId: ProcessStatusLookup.DecisionRequired.Id,
                StageId: LifecycleStageLookup.Changed.Id,
                ActionId: ActionTypeLookup.ActionRequired.Id,
                BaseNote: "decision required - changed qualification",
                IncludeFieldChanges: true,
                IncludeEligibilityReasons: false,
                ReviewRequired: true,
                HasFundingWhichHasNotEnded: context.HasFundingWhichHasNotEnded);
        }

        private static QualificationProcessorOutcome DetermineIneligibleChangeOutcome(
            OutcomeContext context)
        {
            var hasUsageConflict = context.HasApplicationsInProgress || context.HasFundingWhichHasNotEnded;
            var needsDecision = hasUsageConflict;

            var statusId = needsDecision ? ProcessStatusLookup.DecisionRequired.Id : ProcessStatusLookup.NoActionRequired.Id;
            var actionId = needsDecision ? ActionTypeLookup.ActionRequired.Id : ActionTypeLookup.NoActionRequired.Id;
            var stageId = needsDecision ? LifecycleStageLookup.Changed.Id : LifecycleStageLookup.Completed.Id;
            var note = needsDecision
                ? "decision required - changed qualification - conflict or eligibility change"
                : "no action required - changed qualification";

            return new QualificationProcessorOutcome(
                StatusId: statusId,
                StageId: stageId,
                ActionId: actionId,
                BaseNote: note,
                IncludeFieldChanges: context.hasAnyChanges,
                IncludeEligibilityReasons: true,
                ReviewRequired: needsDecision,
                HasFundingWhichHasNotEnded: context.HasFundingWhichHasNotEnded);
        }

        private static QualificationProcessorOutcome DetermineNewQualificationOutcome(OutcomeContext context)
        {
            if (context.IsEligible)
            {
                return new QualificationProcessorOutcome(
                    StatusId: ProcessStatusLookup.DecisionRequired.Id,
                    StageId: LifecycleStageLookup.New.Id,
                    ActionId: ActionTypeLookup.ActionRequired.Id,

                    BaseNote: "decision required - new qualification",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: false,
                    ReviewRequired: true,
                    HasFundingWhichHasNotEnded: false);
            }

            var hasConflict = context.HasApplicationsInProgress;

            var statusId = hasConflict
                ? ProcessStatusLookup.DecisionRequired.Id
                : ProcessStatusLookup.NoActionRequired.Id;

            var actionId = hasConflict
                ? ActionTypeLookup.ActionRequired.Id
                : ActionTypeLookup.NoActionRequired.Id;

            var stageId = hasConflict
                ? LifecycleStageLookup.New.Id
                : LifecycleStageLookup.Completed.Id;

            var baseNote = hasConflict
                ? "decision required - new qualification - active applications"
                : "no action required - new qualification";

            return new QualificationProcessorOutcome(
                StatusId: statusId,
                StageId: stageId,
                ActionId: actionId,
                BaseNote: baseNote,
                IncludeFieldChanges: false,
                IncludeEligibilityReasons: true,
                ReviewRequired: hasConflict,
                HasFundingWhichHasNotEnded: false);
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

            var discussion = new QualificationDiscussionHistory
            {
                Id = Guid.NewGuid(),
                QualificationId = qId,
                ActionTypeId = outcome.ActionId,
                Notes = outcome.BaseNote,
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
            if (existing != null && outcome.HasFundingWhichHasNotEnded)
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
                FundingEligibilityFailedFields = request.IneligibleForFundingFieldNames,
            };
        }
    }
}
