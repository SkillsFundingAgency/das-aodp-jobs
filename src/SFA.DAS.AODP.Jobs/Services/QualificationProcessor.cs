using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class QualificationProcessor : IQualificationProcessor
    {
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


            var outcome = DetermineOutcome(
                existingVersion?.ProcessStatusId,
                existingVersion?.LifecycleStageId,
                existingVersion == null,
                incomingEval.IsEligible,
                hasKeyChanges,
                eligibilityChanged,
                hasActiveApps,
                hasActiveFunding,
                settings
            );

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

        private QualificationProcessorOutcome DetermineOutcome(
            Guid? existingStatusId,
            Guid? existingStageId,
            bool isNew,
            bool isEligible,
            bool hasKeyChanges,
            bool eligibilityChanged,
            bool hasActiveApps,
            bool hasActiveFunding,
            QualificationProcessorSettings settings)
        {
            // 1. New Record Logic
            if (isNew)
            {
                if (isEligible)
                {
                    return new QualificationProcessorOutcome(
                        StatusId: settings.DecisionRequiredStatusId,
                        StageId: settings.NewLifecycleStageId,
                        ActionId: settings.ActionTypeDecisionId,
                        BaseNote: "New Qualification (Eligible) - Decision Required",
                        IncludeFieldChanges: true,
                        IncludeEligibilityReasons: false,
                        ReviewRequired: true,
                        HasFunding: false
                    );
                }

                bool isConflict = hasActiveApps;
                return new QualificationProcessorOutcome(
                    StatusId: isConflict ? settings.DecisionRequiredStatusId : settings.NoActionRequiredStatusId,
                    StageId: settings.NewLifecycleStageId,
                    ActionId: isConflict ? settings.ActionTypeDecisionId : settings.ActionTypeNoActionId,
                    BaseNote: isConflict ? "New Qualification (Ineligible) - Qualification has Active Applications" : "New Qualification (Ineligible) - No Action Required",
                    IncludeFieldChanges: isConflict,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: isConflict,
                    HasFunding: false
                );
            }

            // 2. Ineligible Logic
            if (!isEligible)
            {
                bool hasUsageConflict = hasActiveApps || hasActiveFunding;
                if (hasUsageConflict || eligibilityChanged)
                {
                    return new QualificationProcessorOutcome(
                        StatusId: settings.DecisionRequiredStatusId,
                        StageId: settings.ChangedLifecycleStageId,
                        ActionId: settings.ActionTypeDecisionId,
                        BaseNote: "Review Required: Eligibility/Conflict detected.",
                        IncludeFieldChanges: true,
                        IncludeEligibilityReasons: true,
                        ReviewRequired: true,
                        HasFunding: hasActiveFunding
                    );
                }

                return new QualificationProcessorOutcome(
                    StatusId: settings.NoActionRequiredStatusId,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeNoActionId,
                    BaseNote: "Ineligible - No status change or active conflicts.",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: false,
                    HasFunding: false
                );
            }

            // 3. Approved/Rejected Logic
            if (existingStatusId == settings.ApprovedStatusId || existingStatusId == settings.RejectedStatusId)
            {
                if (hasKeyChanges || eligibilityChanged)
                {
                    return new QualificationProcessorOutcome(
                        StatusId: settings.DecisionRequiredStatusId,
                        StageId: settings.ChangedLifecycleStageId,
                        ActionId: settings.ActionTypeDecisionId,
                        BaseNote: "Key Fields or Eligibility Changed - Re-review required",
                        IncludeFieldChanges: true,
                        IncludeEligibilityReasons: true,
                        ReviewRequired: true,
                        HasFunding: hasActiveFunding
                    );
                }

                return new QualificationProcessorOutcome(
                    StatusId: existingStatusId.Value,
                    StageId: settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeNoActionId,
                    BaseNote: "Minor Data Update - No Review Needed",
                    IncludeFieldChanges: false,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: false,
                    HasFunding: hasActiveFunding
                );
            }

            // 4. In-Review Logic
            if (existingStatusId == settings.OnHoldStatusId || existingStatusId == settings.DecisionRequiredStatusId)
            {
                return new QualificationProcessorOutcome(
                    StatusId: existingStatusId.Value,
                    StageId: existingStageId ?? settings.ChangedLifecycleStageId,
                    ActionId: settings.ActionTypeDecisionId,
                    BaseNote: $"Update to Record In-Review ({(hasKeyChanges ? "Major" : "Minor")})",
                    IncludeFieldChanges: true,
                    IncludeEligibilityReasons: true,
                    ReviewRequired: hasKeyChanges || eligibilityChanged,
                    HasFunding: hasActiveFunding
                );
            }

            // 5. Default Fallback
            return new QualificationProcessorOutcome(
                StatusId: settings.DecisionRequiredStatusId,
                StageId: settings.ChangedLifecycleStageId,
                ActionId: settings.ActionTypeDecisionId,
                BaseNote: "Status Unknown - Review Required",
                IncludeFieldChanges: true,
                IncludeEligibilityReasons: true,
                ReviewRequired: true,
                HasFunding: hasActiveFunding
            );
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
                noteLines.Add($"Changes: {changes?.ChangedFieldsCsv}");
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

        private QualificationVersions CreateQualificationVersion(Guid qualificationId, Guid organisationId, Guid lifecycleStageId,
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