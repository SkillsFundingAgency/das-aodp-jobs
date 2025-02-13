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

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualImportService : IOfqualImportService
    {
        private readonly ILogger<OfqualImportService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IQualificationsService _qualificationsService;
        private readonly IActionTypeService _actionTypeService;
        private Stopwatch _loopCycleStopWatch = new Stopwatch();
        private Stopwatch _processStopWatch = new Stopwatch();

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext,
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService, IActionTypeService actionTypeService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
            _actionTypeService = actionTypeService;
        }

        public async Task StageQualificationsDataAsync(HttpRequestData request)
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(StageQualificationsDataAsync)}] -> Import Ofqual qualifications to staging area...");

            int totalProcessed = 0;
            int pageCount = 1;
            _processStopWatch.Start();

            try
            {
                _loopCycleStopWatch.Restart();

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
        }

        public async Task ProcessQualificationsDataAsync()
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Processing Ofqual Qualifications Staging Data...");

            const int batchSize = 500;
            int processedCount = 0;
            _processStopWatch.Restart();

            try
            {
                var organisationCache = new Dictionary<long, AwardingOrganisation>();
                var qualificationCache = new Dictionary<string, Qualification>();

                var organisationIds = (await _applicationDbContext.AwardingOrganisation
                    .AsNoTracking()
                    .Select(o => o.Ukprn)
                    .ToListAsync())
                    .ToHashSet();

                var qualificationNumbers = (await _applicationDbContext.Qualification
                    .AsNoTracking()
                    .Select(o => o.Qan)
                    .ToListAsync())
                    .ToHashSet();

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

                    var batchOrgIds = batch.Select(q => q.OrganisationId ?? 0).Distinct().ToList();
                    var batchQualNumbers = batch.Select(q => q.QualificationNumberNoObliques).Distinct().ToList();

                    var missingOrgIds = batchOrgIds.Where(id => !organisationCache.ContainsKey(id)).ToList();
                    var missingQualNumbers = batchQualNumbers.Where(qn => !qualificationCache.ContainsKey(qn)).ToList();

                    if (missingOrgIds.Any())
                    {
                        var newOrgs = await _applicationDbContext.AwardingOrganisation
                            .Where(o => missingOrgIds.Contains((int)o.Ukprn))
                            .ToDictionaryAsync(o => o.Ukprn);

                        foreach (var org in newOrgs)
                            organisationCache[Convert.ToInt64(org.Key)] = org.Value;
                    }

                    if (missingQualNumbers.Any())
                    {
                        var newQuals = await _applicationDbContext.Qualification
                            .Where(q => missingQualNumbers.Contains(q.Qan))
                            .ToDictionaryAsync(q => q.Qan);

                        foreach (var qual in newQuals)
                            qualificationCache[qual.Key] = qual.Value;
                    }

                    var newOrganisations = new List<AwardingOrganisation>();
                    var newQualifications = new List<Qualification>();
                    var newQualificationVersions = new List<QualificationVersions>();
                    var newQualificationDiscussions = new List<QualificationDiscussionHistory>();

                    var versionFieldChanges = new List<VersionFieldChange>();
                    var processStatuses = new List<ProcessStatus>();
                    var lifecycleStages = new List<LifecycleStage>();

                    foreach (var qualificationData in batch)
                    {
                        // Check Organization
                        if (!organisationCache.TryGetValue(qualificationData.OrganisationId ?? 0, out var organisation))
                        {
                            organisation = new AwardingOrganisation
                            {
                                Id = Guid.NewGuid(),
                                Ukprn = qualificationData.OrganisationId,
                                RecognitionNumber = qualificationData.OrganisationRecognitionNumber,
                                NameOfqual = qualificationData.OrganisationName,
                                Acronym = qualificationData.OrganisationAcronym
                            };
                            newOrganisations.Add(organisation);
                            organisationCache[qualificationData.OrganisationId ?? 0] = organisation;
                        }

                        // Check Qualification
                        if (!qualificationCache.TryGetValue(qualificationData.QualificationNumberNoObliques, out var qualification))
                        {
                            qualification = new Qualification
                            {
                                Id = Guid.NewGuid(),
                                Qan = qualificationData.QualificationNumberNoObliques,
                                QualificationName = qualificationData.Title
                            };
                            newQualifications.Add(qualification);
                            qualificationCache[qualificationData.QualificationNumberNoObliques] = qualification;
                        }

                        // Check if qualification version exists
                        if (!existingVersionsInfo.TryGetValue(qualification.Id, out var versionInfo))
                            // No existing version - create intial qualification version
                        {
                            var versionFieldChange = new VersionFieldChange
                            {
                                Id = Guid.NewGuid(),
                                QualificationVersionNumber = 1,
                                ChangedFieldNames = null
                            };
                            var processStatus = new ProcessStatus { Id = Guid.NewGuid(), Name = "No Action Required" };
                            var lifecycleStage = new LifecycleStage { Id = Guid.NewGuid(), Name = "New" };

                            var discussionHistory = new QualificationDiscussionHistory
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = qualification.Id,
                                ActionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired),
                                UserDisplayName = "OFQUAL Import",
                                Notes = "No Action Required - New Qualification",
                                Timestamp = DateTime.Now
                            };
                            newQualificationDiscussions.Add(discussionHistory);

                            versionFieldChanges.Add(versionFieldChange);
                            processStatuses.Add(processStatus);
                            lifecycleStages.Add(lifecycleStage);

                            var newQualificationVersion = CreateQualificationVersion(
                                qualification, 
                                organisation, 
                                lifecycleStage, 
                                processStatus, 
                                qualificationData, 
                                versionFieldChange, 
                                1);

                            newQualificationVersions.Add(newQualificationVersion);
                        }
                        else if (!versionInfo.HasChangedFields)
                        {
                            // Existing version without changed fields 
                        }
                        else
                        {
                            // Existing version with changed fields - create new version
                            var versionFieldChange = new VersionFieldChange
                            {
                                Id = Guid.NewGuid(),
                                QualificationVersionNumber = versionInfo.Version + 1,
                                ChangedFieldNames = null
                            };
                            var processStatus = new ProcessStatus { Id = Guid.NewGuid() };
                            var lifecycleStage = new LifecycleStage { Id = Guid.NewGuid() };

                            var discussionHistory = new QualificationDiscussionHistory
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = qualification.Id,
                                ActionTypeId = _actionTypeService.GetActionTypeId(ActionTypeEnum.NoActionRequired),
                                UserDisplayName = "",
                                Notes = "",
                                Timestamp = DateTime.Now
                            };
                            newQualificationDiscussions.Add(discussionHistory);

                            versionFieldChanges.Add(versionFieldChange);
                            processStatuses.Add(processStatus);
                            lifecycleStages.Add(lifecycleStage);

                            var newQualificationVersion = CreateQualificationVersion(
                                qualification,
                                organisation,
                                lifecycleStage,
                                processStatus,
                                qualificationData,
                                versionFieldChange,
                                versionInfo.Version + 1);

                            newQualificationVersions.Add(newQualificationVersion);
                        }
                    }

                    if (versionFieldChanges.Any())
                    {
                        await _applicationDbContext.VersionFieldChanges.AddRangeAsync(versionFieldChanges);
                        await _applicationDbContext.ProcessStatus.AddRangeAsync(processStatuses);
                        await _applicationDbContext.LifecycleStages.AddRangeAsync(lifecycleStages);
                        await _applicationDbContext.SaveChangesAsync();
                    }

                    if (newOrganisations.Any()) await _applicationDbContext.AwardingOrganisation.AddRangeAsync(newOrganisations);
                    if (newQualifications.Any()) await _applicationDbContext.Qualification.AddRangeAsync(newQualifications);
                    if (newQualificationVersions.Any()) await _applicationDbContext.QualificationVersions.AddRangeAsync(newQualificationVersions);
                    if (newQualificationDiscussions.Any()) await _applicationDbContext.QualificationDiscussionHistory.AddRangeAsync(newQualificationDiscussions);

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

        private QualificationVersions CreateQualificationVersion(Qualification qualification, AwardingOrganisation organisation, LifecycleStage lifecycleStage,
            ProcessStatus processStatus, dynamic qualificationData, VersionFieldChange versionFieldChange, int? version)
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

            return new QualificationVersions
            {
                Id = Guid.NewGuid(),
                QualificationId = qualification.Id,
                VersionFieldChangesId = versionFieldChange.Id,
                ProcessStatusId = processStatus.Id,
                AdditionalKeyChangesReceivedFlag = 0,
                LifecycleStageId = lifecycleStage.Id,
                AwardingOrganisationId = organisation.Id,
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
                LifecycleStage = lifecycleStage,
                Organisation = organisation,
                ProcessStatus = processStatus,
                Qualification = qualification,
                VersionFieldChanges = versionFieldChange
            };
        }

    }
}
