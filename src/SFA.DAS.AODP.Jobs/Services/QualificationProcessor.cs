using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class QualificationProcessor : IQualificationProcessor
    {
        private readonly IFundingEligibilityService _fundingService;
        private readonly IChangeDetectionService _changeService;
        private readonly IReferenceDataService _referenceDataService;

        public QualificationProcessor(
            IFundingEligibilityService fundingService,
            IChangeDetectionService changeService,
            IReferenceDataService referenceDataService)
        {
            _fundingService = fundingService;
            _changeService = changeService;
            _referenceDataService = referenceDataService;
        }

        public ProcessingResult? Process(
            QualificationDTO importRecord,
            QualificationVersions? existingVersion,
            Guid qualificationId,
            Guid organisationId,
            bool hasActiveApps,
            bool hasActiveFunding)
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

            var actionReqId = _referenceDataService.GetProcessStatusId(Common.Enum.ProcessStatus.DecisionRequired);
            var noActionId = _referenceDataService.GetProcessStatusId(Common.Enum.ProcessStatus.NoActionRequired);

            var outcome = DetermineOutcome(
                existingVersion?.ProcessStatus?.Name,
                existingVersion?.LifecycleStage?.Name,
                existingVersion == null,
                incomingEval.IsEligible,
                hasKeyChanges,
                eligibilityChanged,
                hasActiveApps,
                hasActiveFunding,
                actionReqId,
                noActionId
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

        private ProcessingOutcome DetermineOutcome(
            string? existingStatus,
            string? existingStage,
            bool isNew,
            bool isEligible,
            bool hasKeyChanges,
            bool eligibilityChanged,
            bool hasActiveApps,
            bool hasActiveFunding,
            Guid actionReqId,
            Guid noActionId)
        {
            if (isNew)
            {
                if (isEligible)
                {
                    return new ProcessingOutcome(Common.Enum.ProcessStatus.DecisionRequired, LifeCycleStage.New, actionReqId, "New Qualification (Eligible) - Decision Required", true, false, false, false);
                }

                bool isConflict = hasActiveApps;
                return new ProcessingOutcome(
                    isConflict ? Common.Enum.ProcessStatus.DecisionRequired : Common.Enum.ProcessStatus.NoActionRequired,
                    LifeCycleStage.New,
                    isConflict ? actionReqId : noActionId,
                    isConflict ? "New Qualification (Ineligible) - Qualification has Active Applications" : "New Qualification (Ineligible) - No Action Required",
                    isConflict, false, true, false
                );
            }

            if (!isEligible)
            {
                bool hasUsageConflict = hasActiveApps || hasActiveFunding;
                if (hasUsageConflict || eligibilityChanged)
                {
                    var reasons = new List<string>();
                    if (hasActiveApps) reasons.Add("Active Applications");
                    if (hasActiveFunding) reasons.Add("Active Funding");
                    if (eligibilityChanged) reasons.Add("Eligibility Status Change");

                    return new ProcessingOutcome(Common.Enum.ProcessStatus.DecisionRequired, LifeCycleStage.Changed, actionReqId, $"Review Required: {string.Join(", ", reasons)}.", true, true, true, hasActiveFunding);
                }

                return new ProcessingOutcome(Common.Enum.ProcessStatus.NoActionRequired, LifeCycleStage.Changed, noActionId, "Ineligible - No status change or active conflicts.", false, true, true, false);
            }

            return existingStatus switch
            {
                Common.Enum.ProcessStatus.Approved or Common.Enum.ProcessStatus.Rejected =>
                    (hasKeyChanges || eligibilityChanged)
                        ? new ProcessingOutcome(Common.Enum.ProcessStatus.DecisionRequired, LifeCycleStage.Changed, actionReqId, "Key Fields or Eligibility Changed - Re-review required", true, true, eligibilityChanged, hasActiveFunding)
                        : new ProcessingOutcome(existingStatus, LifeCycleStage.Changed, noActionId, "Minor Data Update - No Review Needed", false, true, false, hasActiveFunding),

                Common.Enum.ProcessStatus.OnHold or Common.Enum.ProcessStatus.DecisionRequired =>
                    new ProcessingOutcome(existingStatus, existingStage ?? LifeCycleStage.Changed, actionReqId, $"Update to Record In-Review ({(hasKeyChanges ? "Major" : "Minor")})", true, true, hasKeyChanges || eligibilityChanged, hasActiveFunding),

                _ => new ProcessingOutcome(Common.Enum.ProcessStatus.DecisionRequired, LifeCycleStage.Changed, actionReqId, "Status Unknown - Review Required", true, true, true, hasActiveFunding)
            };
        }

        private ProcessingResult BuildResult(
            QualificationDTO import,
            QualificationVersions? existing,
            Guid qId,
            Guid oId,
            ProcessingOutcome outcome,
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

            var newVersion = CreateQualificationVersion(qId, oId, outcome.Stage, outcome.Status, import, fieldChange, eval.IsEligible, versionNumber, eval.GetFailedFieldsCsv());

            QualificationFundingTracker? tracker = null;
            if (existing != null && outcome.HasFunding)
            {
                tracker = new QualificationFundingTracker { OldVersionId = existing.Id, NewVersionId = newVersion.Id };
            }

            return new ProcessingResult(newVersion, discussion, fieldChange, tracker);
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

        private QualificationVersions CreateQualificationVersion(Guid qualificationId, Guid organisationId, string lifecycleStage,
            string processStatus, QualificationDTO qualificationData, VersionFieldChanges versionFieldChange, bool eligibleForFunding,
            int? version, string eligibilityChangeReason)
        {
            var processStatusId = _referenceDataService.GetProcessStatusId(processStatus);
            var lifecycleStageId = _referenceDataService.GetLifecycleStageId(lifecycleStage);

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