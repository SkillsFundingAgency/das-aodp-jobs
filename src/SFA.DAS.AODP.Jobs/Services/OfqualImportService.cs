using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
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
                _logger.LogInformation($"Clearing down StageQualifications table...");

                await _applicationDbContext.TruncateTable<QualificationImportStaging>();

                var parameters = _ofqualRegisterService.ParseQueryParameters(request.Query);

                _logger.LogInformation($"Ofqual data import started...");

                while (true && pageCount < 1000000)
                {
                    parameters.Page = pageCount;

                    var paginatedResult = await _ofqualRegisterService.SearchPrivateQualificationsAsync(parameters);

                    if (paginatedResult.Results == null || !paginatedResult.Results.Any())
                    {
                        _logger.LogInformation("No more qualifications to process.");
                        break;
                    }

                    _logger.LogInformation($"Processing page {pageCount}. Retrieved {paginatedResult.Results?.Count} qualifications.");

                    var importedQualificationsJson = paginatedResult.Results
                        .Select(JsonConvert.SerializeObject)
                        .ToList();

                    await _qualificationsService.AddQualificationsStagingRecords(importedQualificationsJson);

                    totalProcessed += paginatedResult.Results.Count;

                    if (paginatedResult.Results?.Count < parameters.Limit)
                    {
                        _logger.LogInformation("Reached the end of the results set.");
                        break;
                    }

                    _loopCycleStopWatch.Stop();
                    _logger.LogInformation($"Page {pageCount} import complete. {paginatedResult.Results.Count()} records imported in {_loopCycleStopWatch.Elapsed.TotalSeconds:F2} seconds");
                    _loopCycleStopWatch.Restart();
                    pageCount++;
                }

                await _qualificationsService.SaveQualificationsStagingAsync();

                _processStopWatch.Stop();
                _logger.LogInformation($"Successfully imported {totalProcessed} qualifications in {_processStopWatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Unexpected API exception occurred.");
                throw;
            }
            catch (SystemException ex)
            {
                _logger.LogError(ex, "Unexpected system exception occurred.");
                throw;
            }

            return totalProcessed;
        }

        public async Task ProcessQualificationsDataAsync()
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Processing Ofqual Qualifications Staging Data...");

            const int batchSize = 1000;
            int processedCount = 0;
            _processStopWatch.Restart();

            try
            {                                
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
                            
                            if (_fundingEligibilityService.EligibleForFunding(importRecord))
                            {
                                // Eligible for funding - needs decision

                                processStatusName = Enum.ProcessStatus.DecisionRequired;
                                actionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);
                                notes = ImportReason.DecisionRequired;                                
                            }
                            else
                            {
                                // Ineligible for funding - No Action Required                                

                                processStatusName = Enum.ProcessStatus.NoActionRequired;
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
                                                                .OrderByDescending(o => o.Version)
                                                                .AsNoTracking()
                                                                .Where(w => w.QualificationId == qualificationId)
                                                                .FirstOrDefault() ?? throw new Exception($"Unable to location qualification with id {qualificationId} while processing changes");

                            var detectionResults = new DetectionResults();
                            if (currentQualificationVersion != null)
                            {
                                detectionResults = _changeDetectionService.DetectChanges(importRecord, currentQualificationVersion, currentQualificationVersion.Organisation, currentQualificationVersion.Qualification);
                                if (!detectionResults.ChangesPresent) continue;
                            }

                            #region New Version of Existing Qualification

                            if (!_fundingEligibilityService.EligibleForFunding(importRecord))
                            {
                                // Not eligible for funding 
                                
                                var versionFieldChange = new VersionFieldChanges
                                {
                                    Id = Guid.NewGuid(),
                                    QualificationVersionNumber = existingVersion.Version + 1,
                                    ChangedFieldNames = detectionResults.ChangesPresent ? string.Join(", ", detectionResults.Fields) : ""
                                };
                                var processStatusName = Enum.ProcessStatus.NoActionRequired;
                                var lifecycleStageName = LifeCycleStage.Changed;

                                var discussionHistory = new QualificationDiscussionHistory
                                {
                                    Id = Guid.NewGuid(),
                                    QualificationId = qualificationId,
                                    ActionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired),
                                    UserDisplayName = "OFQUAL Import",
                                    Notes = "No Action required - Changed Qualification (Funding Criteria)",
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
                                    existingVersion.Version + 1);

                                newQualificationVersions.Add(newQualificationVersion);
                            }
                            else
                            {
                                // Eligable for funding 

                                var versionFieldChange = new VersionFieldChanges
                                {
                                    Id = Guid.NewGuid(),
                                    QualificationVersionNumber = existingVersion.Version + 1,
                                    ChangedFieldNames = detectionResults.ChangesPresent ? string.Join(", ", detectionResults.Fields) : ""
                                };
                                var processStatusName = Enum.ProcessStatus.DecisionRequired;
                                var lifecycleStageName = LifeCycleStage.Changed;

                                var discussionHistory = new QualificationDiscussionHistory
                                {
                                    Id = Guid.NewGuid(),
                                    QualificationId = qualificationId,
                                    ActionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired),
                                    UserDisplayName = "",
                                    Notes = "",
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
                                    existingVersion.Version + 1);

                                newQualificationVersions.Add(newQualificationVersion);
                            }

                            if (detectionResults.Fields.Contains("Title"))
                            {
                                // update qualification title
                                var qualificationToUpdate = await _applicationDbContext.Qualification
                                    .FirstOrDefaultAsync(q => q.Id == qualificationId);

                                if (qualificationToUpdate != null)
                                {
                                    qualificationToUpdate.QualificationName = importRecord.Title;
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
                }

                _processStopWatch.Stop();
                _logger.LogInformation($"Processed {processedCount} records in {_processStopWatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing qualifications.");
                throw;
            }
        }

        private QualificationVersions CreateQualificationVersion(Guid qualificationId, Guid organisationId, string lifecycleStage,
            string processStatus, QualificationDTO qualificationData, VersionFieldChanges versionFieldChange, int? version)
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
                VersionFieldChanges = versionFieldChange
            };
        }

    }
}
