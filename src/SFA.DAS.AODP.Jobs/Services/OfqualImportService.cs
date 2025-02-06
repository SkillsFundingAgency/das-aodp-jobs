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

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualImportService : IOfqualImportService
    {
        private readonly ILogger<OfqualImportService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IQualificationsService _qualificationsService;
        private Stopwatch _loopCycleStopWatch = new Stopwatch();
        private Stopwatch _processStopWatch = new Stopwatch();

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext,
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
        }

        public async Task StageQualificationsDataAsync(HttpRequestData request)
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(StageQualificationsDataAsync)}] -> Import Ofqual Qualifications to staging area...");

            int totalProcessed = 0;
            int pageCount = 1;
            _processStopWatch.Start();

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

                    pageCount++;
                }

                _processStopWatch.Stop();
                _logger.LogInformation($"Successfully imported {totalProcessed} qualifications in {_processStopWatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Unexpected API exception occurred.");
            }
            catch (SystemException ex)
            {
                _logger.LogError(ex, "Unexpected system exception occurred.");
            }
        }

        public async Task ProcessQualificationsDataAsync()
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Processing Ofqual Qualifications Staging Data...");

            int batchSize = 500;
            int processedCount = 0;
            _processStopWatch.Restart();

            try
            {
                // Pre-fetch existing organisations and qualifications
                var organisationIds = await _applicationDbContext.AwardingOrganisation
                    .AsNoTracking()
                    .Select(o => o.Id)
                    .ToListAsync();

                var qualificationNumbers = await _applicationDbContext.Qualification
                    .AsNoTracking()
                    .Select(o => o.Qan)
                    .ToListAsync();

                while (true && processedCount < 1000000)
                {
                    var batch = await _qualificationsService.GetStagedQualificationsBatchAsync(batchSize, processedCount);
                    if (batch.Count == 0)
                        break;

                    var existingOrganisations = await _applicationDbContext.AwardingOrganisation
                        .Where(o => organisationIds.Contains(o.Id))
                        .ToDictionaryAsync(o => o.Id);

                    var existingQualifications = await _applicationDbContext.Qualification
                        .Where(q => qualificationNumbers.Contains(q.Qan))
                        .ToDictionaryAsync(q => q.Qan);

                    var newOrganisations = new List<AwardingOrganisation>();
                    var newQualifications = new List<Qualification>();
                    var newQualificationVersions = new List<QualificationVersions>();

                    foreach (var qualificationData in batch)
                    {
                        // Fetch or create Organisation
                        if (qualificationData.AwardingOrganisationId.HasValue &&
                            existingOrganisations.TryGetValue(qualificationData.AwardingOrganisationId.Value, out var organisation))
                        {
                            organisation = new AwardingOrganisation
                            {
                                Id = Guid.NewGuid(),
                                RecognitionNumber = qualificationData.OrganisationRecognitionNumber,
                                NameOfqual = qualificationData.OrganisationName,
                                Acronym = qualificationData.OrganisationAcronym
                            };
                            newOrganisations.Add(organisation);
                            existingOrganisations[qualificationData.AwardingOrganisationId.Value] = organisation;
                        }

                        // Fetch or create Qualification
                        if (!existingQualifications.TryGetValue(qualificationData.QualificationNumberNoObliques, out var qualification))
                        {
                            qualification = new Qualification
                            {
                                Id = Guid.NewGuid(),
                                Qan = qualificationData.QualificationNumberNoObliques,
                                QualificationName = qualificationData.Title
                            };
                            newQualifications.Add(qualification);
                            existingQualifications[qualificationData.QualificationNumberNoObliques] = qualification;
                        }

                        // Check Qualification Version
                        var qualificationVersionExists = await _applicationDbContext.QualificationVersions
                            .AnyAsync(qv => qv.QualificationId == qualificationData.Id);

                        if (!qualificationVersionExists)
                        {
                            var versionFieldChanges = new VersionFieldChange { Id = Guid.NewGuid(), QualificationVersionNumber = 1 };
                            var processStatus = new ProcessStatus { Id = Guid.NewGuid() };
                            var lifecycleStage = new LifecycleStage { Id = Guid.NewGuid() };

                            await _applicationDbContext.VersionFieldChanges.AddAsync(versionFieldChanges);
                            await _applicationDbContext.ProcessStatus.AddAsync(processStatus);
                            await _applicationDbContext.LifecycleStages.AddAsync(lifecycleStage);
                            await _applicationDbContext.SaveChangesAsync(); // Save these first to get their IDs

                            var qualificationVersion = new QualificationVersions
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = qualification.Id,
                                VersionFieldChangesId = versionFieldChanges.Id,
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
                                AssessmentMethods = qualificationData.AssessmentMethods != null
                                    ? string.Join(", ", qualificationData.AssessmentMethods)
                                    : string.Empty,
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
                                Version = 1,
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
                                VersionFieldChanges = versionFieldChanges
                            };

                            newQualificationVersions.Add(qualificationVersion);
                        }
                    }

                    // Bulk insert all new entities
                    if (newOrganisations.Any()) await _applicationDbContext.AwardingOrganisation.AddRangeAsync(newOrganisations);
                    if (newQualifications.Any()) await _applicationDbContext.Qualification.AddRangeAsync(newQualifications);
                    if (newQualificationVersions.Any()) await _applicationDbContext.QualificationVersions.AddRangeAsync(newQualificationVersions);

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

    }
}
