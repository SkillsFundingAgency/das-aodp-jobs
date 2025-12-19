using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestEase;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Models.Qualification;
using System.Diagnostics;
using System.Text.Json;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualImportService : IOfqualImportService
    {
        private readonly ILogger<OfqualImportService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IQualificationsService _qualificationsService;
        private readonly IReferenceDataService _actionTypeService;
        private readonly IFundingEligibilityService _fundingEligibilityService;
        private readonly IChangeDetectionService _changeDetectionService;
        private Stopwatch _loopCycleStopWatch = new Stopwatch();
        private Stopwatch _processStopWatch = new Stopwatch();

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext,
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService, 
            IReferenceDataService actionTypeService, IFundingEligibilityService fundingEligibilityService,
            IChangeDetectionService changeDetectionService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
            _actionTypeService = actionTypeService;
            _fundingEligibilityService = fundingEligibilityService;
            _changeDetectionService = changeDetectionService;
        }

        public async Task<int> ImportApiData(HttpRequestData request)
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Import Ofqual qualifications to staging area...");

            int totalProcessed = 0;
            int pageCount = 1;
            _processStopWatch.Start();
            _loopCycleStopWatch.Start();
            try
            {                
                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Clearing down StageQualifications table...");

                await _applicationDbContext.Truncate_QualificationImportStaging();

                var parameters = _ofqualRegisterService.ParseQueryParameters(request.Query);

                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Starting Ofqual data import...");

                while (true && pageCount < 1000000)
                {
                    parameters.Page = pageCount;

                    var paginatedResult = await _ofqualRegisterService.SearchPrivateQualificationsAsync(parameters);

                    if (paginatedResult.Results == null || !paginatedResult.Results.Any())
                    {
                        _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> No more qualifications to process.");
                        break;
                    }

                    _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Processing page {pageCount}. Retrieved {paginatedResult.Results?.Count} qualifications.");

                    var importedQualificationsJson = paginatedResult.Results
                        .Select(JsonConvert.SerializeObject)
                        .ToList();

                    await _qualificationsService.AddQualificationsStagingRecords(importedQualificationsJson);

                    totalProcessed += paginatedResult.Results.Count;

                    if (paginatedResult.Results?.Count < parameters.Limit)
                    {
                        _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Reached the end of the results set.");
                        break;
                    }

                    _loopCycleStopWatch.Stop();
                    Thread.Sleep(200);
                    _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Page {pageCount} import complete. {paginatedResult.Results.Count()} records imported in {_loopCycleStopWatch.Elapsed.TotalSeconds:F2} seconds");
                    _loopCycleStopWatch.Restart();
                    pageCount++;
                }

                await _qualificationsService.SaveQualificationsStagingAsync();

                _processStopWatch.Stop();
                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Successfully imported {totalProcessed} qualifications in {_processStopWatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Unexpected API exception occurred.");
                throw;
            }
            catch (SystemException ex)
            {
                _logger.LogError(ex, $"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Unexpected system exception occurred.");
                throw;
            }

            return totalProcessed;
        }

        public async Task ProcessQualificationsDataAsync()
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Processing Ofqual Qualifications Staging Data...");

            const int batchSize = 500;
            int processedCount = 0;
            _processStopWatch.Restart();

            try
            {
                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Building existing qualification, organisation and qualifcation version caches...");
                var fundingsToBeUpdated = new List<QualificationFundingTracker>();

                var organisationCache = (await _applicationDbContext.AwardingOrganisation
                    .AsNoTracking()
                    .Where(w => w.Ukprn.HasValue)
                    .Select(o => new { Ukprn = o.Ukprn ?? 0, o.Id })
                    .ToListAsync())
                    .ToDictionary(a => a.Ukprn, a => a.Id);

                var qualificationCache = (await _applicationDbContext.Qualification
                    .AsNoTracking()
                    .Select(o => new { Qan = o.Qan, Id = o.Id, Title = o.QualificationName })
                    .ToListAsync())
                    .ToDictionary(a => a.Qan, a => new { Id = a.Id, Title = a.Title });

                var existingVersionsCache = (await _applicationDbContext.QualificationVersions
                    .Include(qv => qv.VersionFieldChanges)
                    .GroupBy(g => g.QualificationId)
                    .AsNoTracking()
                    .Select(qv => new
                    {
                        QualificationId = qv.Key,
                        LatestVersion = qv.OrderByDescending(o => o.Version).First(),
                    })
                    .ToListAsync())
                    .Select(s => new
                    {
                        QualificationId = s.QualificationId,
                        Version = s.LatestVersion.Version,
                        HasChangedFields = s.LatestVersion != null && s.LatestVersion.VersionFieldChanges != null && !string.IsNullOrEmpty(s.LatestVersion.VersionFieldChanges.ChangedFieldNames),
                        ChangedFields = s.LatestVersion?.VersionFieldChanges?.ChangedFieldNames
                    })                    
                    .ToDictionary(x => x.QualificationId, x => new
                    {
                        x.HasChangedFields,
                        x.Version,
                        x.ChangedFields
                    });

                while (processedCount < 1000000)
                {
                    var importRecords = await _qualificationsService.GetStagedQualificationsBatchAsync(batchSize, processedCount);
                    if (!importRecords.Any()) break;                                  

                    var newOrganisations = new List<AwardingOrganisation>();
                    var newQualifications = new List<Qualification>();
                    var newQualificationVersions = new List<QualificationVersions>();
                    var newQualificationDiscussions = new List<QualificationDiscussionHistory>();
                    var updatedQualifications = new List<Qualification>();
                    var updatedQualificationFundings = new List<QualificationFunding>();
                    var updatedQualificationFeedbacks = new List<QualificationFundingFeedback>();

                    var versionFieldChanges = new List<VersionFieldChanges>();
                    var processStatuses = new List<Data.Entities.ProcessStatus>();
                    var lifecycleStages = new List<LifecycleStage>();

                    foreach (var importRecord in importRecords)
                    {
                        // Check Organization
                        var organisationId = Guid.Empty;
                        if (!organisationCache.ContainsKey(importRecord.OrganisationId ?? 0))
                        {
                            organisationId = Guid.NewGuid();
                            var organisation = new AwardingOrganisation
                            {
                                Id = organisationId,
                                Ukprn = importRecord.OrganisationId,
                                RecognitionNumber = importRecord.OrganisationRecognitionNumber,
                                NameOfqual = importRecord.OrganisationName,
                                NameLegal = importRecord.OrganisationName,
                                Acronym = importRecord.OrganisationAcronym
                            };
                            newOrganisations.Add(organisation);
                            organisationCache[importRecord.OrganisationId ?? 0] = organisationId;
                        }
                        else
                        { 
                            organisationId = organisationCache[importRecord.OrganisationId ?? 0]; 
                        }

                        // Check Qualification
                        var qualificationId = Guid.Empty;
                        var qan = importRecord.QualificationNumberNoObliques ?? "";

                        if (!qualificationCache.ContainsKey(qan))
                        {
                            qualificationId = Guid.NewGuid();
                            var qualification = new Qualification
                            {
                                Id = qualificationId,
                                Qan = importRecord.QualificationNumberNoObliques ?? "",
                                QualificationName = importRecord.Title
                            };
                            newQualifications.Add(qualification);
                            qualificationCache[qan] = new { Id = qualificationId, Title = importRecord.Title };
                        }
                        else
                        {
                            var cachedQualification = qualificationCache[qan];
                            qualificationId = cachedQualification.Id;
                        }

                        // Check if qualification version exists
                        if (!existingVersionsCache.TryGetValue(qualificationId, out var existingVersion))
                        {
                            #region New Qualification

                            var notes = "";
                            var processStatusName = "";                       
                            var actionTypeId = Guid.Empty;

                            var eligibleForFunding = _fundingEligibilityService.EligibleForFunding(importRecord);

                            if (eligibleForFunding)
                            {
                                // Eligible for funding - needs decision

                                processStatusName = Common.Enum.ProcessStatus.DecisionRequired;
                                actionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);
                                notes = ImportReason.DecisionRequired;                                
                            }
                            else
                            {
                                // Ineligible for funding - No Action Required                                

                                processStatusName = Common.Enum.ProcessStatus.NoActionRequired;
                                actionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired);
                                notes = _fundingEligibilityService.DetermineFailureReason(importRecord);                                
                            }

                            var versionFieldChange = new VersionFieldChanges
                            {
                                Id = Guid.NewGuid(),
                                QualificationVersionNumber = 1,
                                ChangedFieldNames = null
                            };
                            
                            var lifecycleStage = LifeCycleStage.New;

                            var discussionHistory = new QualificationDiscussionHistory
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = qualificationId,
                                ActionTypeId = actionTypeId,
                                UserDisplayName = "OFQUAL Import",
                                Notes = notes,
                                Timestamp = DateTime.Now
                            };
                            newQualificationDiscussions.Add(discussionHistory);

                            versionFieldChanges.Add(versionFieldChange);                            

                            var newQualificationVersion = CreateQualificationVersion(
                                qualificationId,
                                organisationId,
                                lifecycleStage,
                                processStatusName,
                                importRecord,
                                versionFieldChange,
                                eligibleForFunding,
                                1);

                            newQualificationVersions.Add(newQualificationVersion);

                            #endregion

                        }
                        else
                        {
                            // We have a previous version

                            // check for changed fields
                            var currentQualificationDto = new QualificationDTO();
                            var currentQualificationVersion = _applicationDbContext.QualificationVersions
                                                                .Include(i => i.Qualification)
                                                                .Include(i => i.Organisation)
                                                                .Include(i => i.ProcessStatus)
                                                                .Include(i => i.LifecycleStage)                                                                
                                                                .OrderByDescending(o => o.Version)
                                                                .AsNoTracking()
                                                                .Where(w => w.QualificationId == qualificationId)
                                                                .FirstOrDefault() ?? throw new Exception($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Unable to location qualification with id {qualificationId} while processing changes");

                            var detectionResults = new DetectionResults();
                            if (currentQualificationVersion != null)
                            {
                                detectionResults = _changeDetectionService.DetectChanges(importRecord, currentQualificationVersion, currentQualificationVersion.Organisation, currentQualificationVersion.Qualification);
                                if (!detectionResults.ChangesPresent) continue;
                            }

                            var processStatusName = Common.Enum.ProcessStatus.NoActionRequired;
                            var lifecycleStageName = LifeCycleStage.Changed;
                            var actionId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired);
                            var notes = "";

                            #region New Version of Existing Qualification
                            var eligibleForFunding = _fundingEligibilityService.EligibleForFunding(importRecord);
                            if (!eligibleForFunding)
                            {
                                // Not eligible for funding 

                                processStatusName = Common.Enum.ProcessStatus.NoActionRequired;
                                lifecycleStageName = LifeCycleStage.Changed;
                                actionId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired);
                                notes = "No Action required - Changed Qualification (Funding Criteria)";
                            }
                            else
                            {
                                notes = "Decision Required - Changed Qualification";
                                if ((currentQualificationVersion.ProcessStatus.Name == Common.Enum.ProcessStatus.Approved) ||
                                        (currentQualificationVersion.ProcessStatus.Name == Common.Enum.ProcessStatus.Rejected))
                                {

                                    if (detectionResults.KeyFieldsChanged)
                                    {
                                        // Decision required as major changes
                                        processStatusName = Common.Enum.ProcessStatus.DecisionRequired;
                                        notes = "Decision Required - Changed Qualification (Key Fields)";
                                    }
                                    else
                                    {
                                        // Keep the current status as only minor changes
                                        processStatusName = currentQualificationVersion.ProcessStatus.Name;
                                        notes = "Decision Required - Changed Qualification (Minor Fields)";
                                    }

                                    lifecycleStageName = LifeCycleStage.Changed;
                                    actionId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);                                    
                                }
                                else if ((currentQualificationVersion.ProcessStatus.Name == Common.Enum.ProcessStatus.OnHold) ||
                                        (currentQualificationVersion.ProcessStatus.Name == Common.Enum.ProcessStatus.DecisionRequired))
                                {
                                    // Keep the current status as only changed dont matter when on hold/decision required
                                    processStatusName = currentQualificationVersion.ProcessStatus.Name;
                                    lifecycleStageName = currentQualificationVersion.LifecycleStage.Name;
                                    if (detectionResults.KeyFieldsChanged)
                                    {                                        
                                        
                                        notes = currentQualificationVersion.ProcessStatus.Name == Common.Enum.ProcessStatus.OnHold ?
                                            "On Hold - Changed Qualification (Key Fields)" :
                                            "Decision Required - Changed Qualification (Key Fields)";                                        
                                    }
                                    else
                                    {
                                        notes = "Decision Required - Changed Qualification (Minor Fields)";
                                    }
                                    
                                    actionId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);
                                }
                                else
                                {
                                    processStatusName = Common.Enum.ProcessStatus.DecisionRequired;
                                    lifecycleStageName = LifeCycleStage.Changed;
                                    actionId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);                                   
                                }
                            }

                            var versionFieldChange = new VersionFieldChanges
                            {
                                Id = Guid.NewGuid(),
                                QualificationVersionNumber = existingVersion.Version + 1,
                                ChangedFieldNames = detectionResults.ChangesPresent ? string.Join(", ", detectionResults.Fields) : ""
                            };

                            var discussionHistory = new QualificationDiscussionHistory
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = qualificationId,
                                ActionTypeId = actionId,
                                UserDisplayName = "OFQUAL Import",
                                Notes = notes,
                                Timestamp = DateTime.Now
                            };
                            newQualificationDiscussions.Add(discussionHistory);

                            versionFieldChanges.Add(versionFieldChange);

                            var newQualificationVersion = CreateQualificationVersion(
                                qualificationId,
                                organisationId,
                                lifecycleStageName,
                                processStatusName,
                                importRecord,
                                versionFieldChange,
                                eligibleForFunding,
                                existingVersion.Version + 1);

                            newQualificationVersions.Add(newQualificationVersion);

                            if (detectionResults.Fields.Contains("Title"))
                            {
                                // update qualification title
                                var qualificationToUpdate = await _applicationDbContext.Qualification
                                    .FirstOrDefaultAsync(q => q.Id == qualificationId);

                                if (qualificationToUpdate != null)
                                {
                                    qualificationToUpdate.QualificationName = importRecord.Title;
                                    updatedQualifications.Add(qualificationToUpdate);
                                }

                            }

                            var currentProcessStatus = currentQualificationVersion.ProcessStatus.Name;
                            if (currentProcessStatus != Common.Enum.ProcessStatus.Approved 
                                && currentProcessStatus != Common.Enum.ProcessStatus.Rejected)                                
                            {
                                var fundingsPresent = await CheckForPreviousFundings(currentQualificationVersion.Id);
                                if (fundingsPresent)
                                {
                                    var tracker = new QualificationFundingTracker() 
                                    { 
                                        OldVersionId = currentQualificationVersion.Id,
                                        NewVersionId = newQualificationVersion.Id
                                    };

                                    fundingsToBeUpdated.Add(tracker);
                                }
                            }
                            #endregion  
                        }
                    }

                    if (newOrganisations.Any()) await _applicationDbContext.AwardingOrganisation.AddRangeAsync(newOrganisations);
                    if (newQualifications.Any()) await _applicationDbContext.Qualification.AddRangeAsync(newQualifications);
                    if (newQualificationVersions.Any()) await _applicationDbContext.QualificationVersions.AddRangeAsync(newQualificationVersions);
                    if (newQualificationDiscussions.Any()) await _applicationDbContext.QualificationDiscussionHistory.AddRangeAsync(newQualificationDiscussions);
                    if (versionFieldChanges.Any()) await _applicationDbContext.VersionFieldChanges.AddRangeAsync(versionFieldChanges);

                    await _applicationDbContext.SaveChangesAsync();

                    processedCount += importRecords.Count;
                    Thread.Sleep(200);
                }

                if (fundingsToBeUpdated.Any())
                {
                    _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ImportApiData)}] -> Moving {fundingsToBeUpdated.Count} Qual Funding records from old versions to new");
                    // Update any qualifications that need funding records moved from old version to new
                    foreach (var tracker in fundingsToBeUpdated)
                    {
                        var updatedFunding = await UpdateFundings(tracker.OldVersionId, tracker.NewVersionId);
                        var updatedFundingFeedback = await UpdateFundingFeedbacks(tracker.OldVersionId, tracker.NewVersionId);
                    }
                    await _applicationDbContext.SaveChangesAsync();
                }

                _processStopWatch.Stop();
                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Processed {processedCount} records in {_processStopWatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Error processing qualifications.");
                throw;
            }
        }

        private QualificationVersions CreateQualificationVersion(Guid qualificationId, Guid organisationId, string lifecycleStage,
            string processStatus, QualificationDTO qualificationData, VersionFieldChanges versionFieldChange, bool eligibleForFunding, int? version)
        {
            string GetJoinedArrayOrEmpty(JsonElement? value)
            {
                if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Null)
                    return string.Empty;

                try
                {
                    if (value.Value.ValueKind == JsonValueKind.Array)
                    {
                        var items = new List<string>();
                        foreach (var item in value.Value.EnumerateArray())
                        {
                            items.Add(item.ToString());
                        }
                        return string.Join(", ", items);
                    }
                    return string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }

            var processStatusId = _actionTypeService.GetProcessStatusId(processStatus);
            var lifecycleStageId = _actionTypeService.GetLifecycleStageId(lifecycleStage);

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
                AssessmentMethods = GetJoinedArrayOrEmpty((JsonElement?)qualificationData.AssessmentMethods),
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
                Name = qualificationData.Title
            };
        }

        private async Task<bool> CheckForPreviousFundings(Guid currentQualificationVersionId)
        {
            return await _applicationDbContext.QualificationFundings.Where(w => w.QualificationVersionId == currentQualificationVersionId).AnyAsync();
        }

        private async Task<List<QualificationFunding>> UpdateFundings(Guid currentQualificationVersionId, Guid newQualificationVersionId)
        {
            var fundings = await _applicationDbContext.QualificationFundings
                            .Where(w => w.QualificationVersionId == currentQualificationVersionId)
                            .ToListAsync();
            foreach(var funding in fundings)
            {
                funding.QualificationVersionId = newQualificationVersionId;               
            }

            return fundings;
        }

        private async Task<List<QualificationFundingFeedback>> UpdateFundingFeedbacks(Guid currentQualificationVersionId, Guid newQualificationVersionId)
        {
            var fundingFeedbacks = await _applicationDbContext.QualificationFundingFeedbacks
                            .Where(w => w.QualificationVersionId == currentQualificationVersionId)
                            .ToListAsync();
            foreach (var funding in fundingFeedbacks)
            {
                funding.QualificationVersionId = newQualificationVersionId;
            }

            return fundingFeedbacks;
        }
    }
}
