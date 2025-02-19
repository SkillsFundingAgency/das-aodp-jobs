using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Interfaces;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Infrastructure.Context;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Jobs.Enum;
using System.Text.Json;
using System.Linq;

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
        private Stopwatch _loopCycleStopWatch = new Stopwatch();
        private Stopwatch _processStopWatch = new Stopwatch();

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext,
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService, 
            IReferenceDataService actionTypeService, IFundingEligibilityService fundingEligibilityService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
            _actionTypeService = actionTypeService;
            _fundingEligibilityService = fundingEligibilityService;
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

                    await _qualificationsService.SaveQualificationsStagingAsync(importedQualificationsJson);

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

            const int batchSize = 500;
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
                    .Select(o => new { Qan = o.Qan, Id = o.Id })
                    .ToListAsync())
                    .ToDictionary(a => a.Qan, a => a.Id);

                var existingVersionsInfo = await _applicationDbContext.QualificationVersions
                    .AsNoTracking()
                    .Include(qv => qv.VersionFieldChanges)
                    .Select(qv => new
                    {
                        QualificationId = qv.QualificationId,
                        HasChangedFields = qv.VersionFieldChanges.ChangedFieldNames != null &&
                                         qv.VersionFieldChanges.ChangedFieldNames.Length > 0,
                        Version = qv.Version
                    })
                    .ToDictionaryAsync(x => x.QualificationId, x => new
                    {
                        x.HasChangedFields,
                        x.Version
                    });

                while (processedCount < 1000000)
                {
                    var batch = await _qualificationsService.GetStagedQualificationsBatchAsync(batchSize, processedCount);
                    if (!batch.Any()) break;                                  

                    var newOrganisations = new List<AwardingOrganisation>();
                    var newQualifications = new List<Qualification>();
                    var newQualificationVersions = new List<QualificationVersions>();
                    var newQualificationDiscussions = new List<QualificationDiscussionHistory>();

                    var versionFieldChanges = new List<VersionFieldChange>();
                    var processStatuses = new List<Data.Entities.ProcessStatus>();
                    var lifecycleStages = new List<LifecycleStage>();

                    foreach (var qualificationData in batch)
                    {
                        // Check Organization
                        var organisationId = Guid.Empty;
                        if (!organisationCache.ContainsKey(qualificationData.OrganisationId ?? 0))
                        {
                            organisationId = Guid.NewGuid();
                            var organisation = new AwardingOrganisation
                            {
                                Id = organisationId,
                                Ukprn = qualificationData.OrganisationId,
                                RecognitionNumber = qualificationData.OrganisationRecognitionNumber,
                                NameOfqual = qualificationData.OrganisationName,
                                Acronym = qualificationData.OrganisationAcronym
                            };
                            newOrganisations.Add(organisation);
                            organisationCache[qualificationData.OrganisationId ?? 0] = organisationId;
                        }
                        else
                        { 
                            organisationId = organisationCache[qualificationData.OrganisationId ?? 0]; 
                        }

                        // Check Qualification
                        var qualificationId = Guid.Empty;
                        if (!qualificationCache.ContainsKey(qualificationData.QualificationNumberNoObliques ?? ""))
                        {
                            qualificationId = Guid.NewGuid();
                            var qualification = new Qualification
                            {
                                Id = qualificationId,
                                Qan = qualificationData.QualificationNumberNoObliques,
                                QualificationName = qualificationData.Title
                            };
                            newQualifications.Add(qualification);
                            qualificationCache[qualificationData.QualificationNumberNoObliques ?? ""] = qualificationId;
                        }
                        else
                        {
                            qualificationId = qualificationCache[qualificationData.QualificationNumberNoObliques ?? ""];
                        }

                        // Check if qualification version exists
                        if (!existingVersionsInfo.TryGetValue(qualificationId, out var versionInfo))
                        {
                            #region New Qualification

                            var notes = "";
                            var processStatusName = "";                       
                            var actionTypeId = Guid.Empty;
                            
                            if (_fundingEligibilityService.EligibleForFunding(qualificationData))
                            {
                                // Eligible for funding - needs decision

                                processStatusName = Enum.ProcessStatus.DecisionRequired;
                                actionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.ActionRequired);
                                notes = ImportReason.DecisionRequired;                                
                            }
                            else
                            {
                                //New Qualification ineligible for funding - No Action Required                                

                                processStatusName = Enum.ProcessStatus.NoActionRequired;
                                actionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired);
                                notes = _fundingEligibilityService.DetermineFailureReason(qualificationData);                                
                            }

                            var versionFieldChange = new VersionFieldChange
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
                                qualificationData,
                                versionFieldChange,
                                1);

                            newQualificationVersions.Add(newQualificationVersion);

                            #endregion

                        }
                        else if (!versionInfo.HasChangedFields)
                        {
                            // Existing version without changed fields 
                        }
                        else
                        {
                            // Existing version with changed fields
                            var versionFieldChange = new VersionFieldChange
                            {
                                Id = Guid.NewGuid(),
                                QualificationVersionNumber = versionInfo.Version + 1,
                                ChangedFieldNames = null
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
                                qualificationData,
                                versionFieldChange,
                                versionInfo.Version + 1);

                            newQualificationVersions.Add(newQualificationVersion);
                        }
                    }                    

                    if (newOrganisations.Any()) await _applicationDbContext.AwardingOrganisation.AddRangeAsync(newOrganisations);
                    if (newQualifications.Any()) await _applicationDbContext.Qualification.AddRangeAsync(newQualifications);
                    if (newQualificationVersions.Any()) await _applicationDbContext.QualificationVersions.AddRangeAsync(newQualificationVersions);
                    if (newQualificationDiscussions.Any()) await _applicationDbContext.QualificationDiscussionHistory.AddRangeAsync(newQualificationDiscussions);
                    if (versionFieldChanges.Any())
                    {
                        await _applicationDbContext.VersionFieldChanges.AddRangeAsync(versionFieldChanges);
                        //await _applicationDbContext.SaveChangesAsync();
                    }

                    await _applicationDbContext.SaveChangesAsync();

                    processedCount += batch.Count;
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
            string processStatus, dynamic qualificationData, VersionFieldChange versionFieldChange, int? version)
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
