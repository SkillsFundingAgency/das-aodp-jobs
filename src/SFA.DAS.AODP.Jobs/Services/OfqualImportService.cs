using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualImportService : IOfqualImportService
    {
        private readonly ILogger<OfqualImportService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IQualificationsService _qualificationsService;
        private readonly IQualificationProcessor _qualificationProcessor;
        private Stopwatch _loopCycleStopWatch = new Stopwatch();
        private Stopwatch _processStopWatch = new Stopwatch();
        private readonly ISystemClockService _clockService ;

        private static readonly string[] ActiveApplicationStatuses =
        {
            "InReview",
            "Reviewed",
            "OnHold"
        };

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext,
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService,
            IQualificationProcessor qualificationProcessor, ISystemClockService clockService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
            _qualificationProcessor = qualificationProcessor;
            _clockService = clockService;
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

            const int batchSize = 1000;
            int processedCount = 0;
            _processStopWatch.Restart();

            try
            {
                _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(ProcessQualificationsDataAsync)}] -> Building existing qualification, organisation and qualifcation version caches...");
                var fundingsToBeUpdated = new List<QualificationFundingTracker>();

                var organisationCache = (await _applicationDbContext.AwardingOrganisation
                        .AsNoTracking()
                        .Where(w => w.Ukprn.HasValue && w.Ukprn.Value > 0)
                        .Select(o => new
                        {
                            o.Ukprn,
                            o.Id,
                            o.NameOfqual,
                            o.NameLegal,
                            o.Acronym,
                            o.RecognitionNumber
                        })
                        .ToListAsync())
                    .ToDictionary(
                        x => x.Ukprn!.Value,
                        x => new OrganisationCacheItem(
                            x.Id,
                            x.NameOfqual,
                            x.NameLegal,
                            x.Acronym,
                            x.RecognitionNumber
                        )
                    );

                var qualificationCache = (await _applicationDbContext.Qualification
                    .AsNoTracking()
                    .Select(o => new { Qan = o.Qan, Id = o.Id, Title = o.QualificationName ?? string.Empty })
                    .ToListAsync())
                    .ToDictionary(a => a.Qan, a => new { Id = a.Id, Title = a.Title });

                var latestQualificationVersion = await _applicationDbContext.QualificationVersions
                    .AsNoTracking()
                    .Include(qv => qv.Qualification)
                    .Include(qv => qv.Organisation)
                    .Include(qv => qv.VersionFieldChanges)
                    .ToListAsync();

                var activeApplicationsList = _applicationDbContext.Applications
                    .Where(a => ActiveApplicationStatuses.Contains(a.Status))
                    .Select(a => a.QualificationNumber);

                var notEndedQualificationIds = _applicationDbContext.QualificationFundings
                    .Where(f => !f.EndDate.HasValue || f.EndDate.Value > _clockService.Today)
                    .Select(f => f.QualificationVersion.Qualification.Id)
                    .Distinct();

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
                        if (!importRecord.OrganisationId.HasValue || importRecord.OrganisationId.Value <= 0)
                        {
                            _logger.LogWarning("Invalid OrganisationId for QAN {Qan} in Ofqual import data", importRecord.QualificationNumberNoObliques);
                            continue;
                        }

                        //This is stored as Ukprn in our system but is not actually the UKPRN from the API, it is just a unique identifier for the awarding organisation.
                        var organisationIdentifier = importRecord.OrganisationId.Value;

                        if (!organisationCache.TryGetValue(organisationIdentifier, out var cachedOrganisation))
                        {
                            // New organisation
                            organisationId = Guid.NewGuid();

                            var organisation = new AwardingOrganisation
                            {
                                Id = organisationId,
                                Ukprn = organisationIdentifier,
                                RecognitionNumber = importRecord.OrganisationRecognitionNumber,
                                NameOfqual = importRecord.OrganisationName,
                                NameLegal = importRecord.OrganisationName,
                                Acronym = importRecord.OrganisationAcronym
                            };

                            newOrganisations.Add(organisation);

                            organisationCache[organisationIdentifier] = new OrganisationCacheItem(
                                organisationId,
                                importRecord.OrganisationName,
                                importRecord.OrganisationName,
                                importRecord.OrganisationAcronym,
                                importRecord.OrganisationRecognitionNumber
                            );
                        }
                        else
                        {
                            organisationId = cachedOrganisation.Id;

                            bool organisationChanged =
                                cachedOrganisation.NameOfqual != importRecord.OrganisationName ||
                                cachedOrganisation.NameLegal != importRecord.OrganisationName ||
                                cachedOrganisation.Acronym != importRecord.OrganisationAcronym ||
                                cachedOrganisation.RecognitionNumber != importRecord.OrganisationRecognitionNumber;

                            if (organisationChanged)
                            {
                                var organisationToUpdate = await _applicationDbContext.AwardingOrganisation
                                    .FirstOrDefaultAsync(o => o.Id == organisationId);

                                if (organisationToUpdate != null)
                                {
                                    organisationToUpdate.NameOfqual = importRecord.OrganisationName;
                                    organisationToUpdate.NameLegal = importRecord.OrganisationName;
                                    organisationToUpdate.Acronym = importRecord.OrganisationAcronym;
                                    organisationToUpdate.RecognitionNumber = importRecord.OrganisationRecognitionNumber;
                                }

                                organisationCache[organisationIdentifier] = new OrganisationCacheItem(
                                    organisationId,
                                    importRecord.OrganisationName,
                                    importRecord.OrganisationName,
                                    importRecord.OrganisationAcronym,
                                    importRecord.OrganisationRecognitionNumber
                                );
                            }
                        }
                        
                        #region Resolve Qualification
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

                            if (importRecord.Title != cachedQualification.Title)
                            {
                                var existingQual = await _applicationDbContext.Qualification
                                    .FirstOrDefaultAsync(q => q.Id == qualificationId);

                                if (existingQual != null)
                                {
                                    existingQual.QualificationName = importRecord.Title;
                                }

                                qualificationCache[qan] = new { Id = qualificationId, Title = importRecord.Title };
                            }
                        }
                        #endregion Resolve Qualification

                        bool hasApplicationsInProgress = await activeApplicationsList.ContainsAsync(importRecord.QualificationNumberNoObliques) ||
                            await activeApplicationsList.ContainsAsync(importRecord.QualificationNumber);
                        bool hasFundingWhichHasNotEnded = await notEndedQualificationIds.ContainsAsync(qualificationId);
                        var currentQualificationVersion = latestQualificationVersion.Where(o => o.QualificationId == qualificationId).OrderByDescending(x => x.Version).FirstOrDefault();

                        var result = _qualificationProcessor.Process(
                            importRecord,
                            currentQualificationVersion,
                            qualificationId,
                            organisationId,
                            hasApplicationsInProgress, 
                            hasFundingWhichHasNotEnded
                        );

                        if (result != null)
                        {
                            newQualificationVersions.Add(result.NewVersion);
                            newQualificationDiscussions.Add(result.Discussion);
                            versionFieldChanges.Add(result.FieldChange);

                            if (result.FundingTracker != null)
                            {
                                fundingsToBeUpdated.Add(result.FundingTracker);
                            }
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
        private async Task<List<QualificationFunding>> UpdateFundings(Guid currentQualificationVersionId, Guid newQualificationVersionId)
        {
            var fundings = await _applicationDbContext.QualificationFundings
                            .Where(w => w.QualificationVersionId == currentQualificationVersionId)
                            .ToListAsync();
            foreach (var funding in fundings)
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

        private sealed record OrganisationCacheItem(
            Guid Id,
            string? NameOfqual,
            string? NameLegal,
            string? Acronym,
            string? RecognitionNumber
        );
    }
}