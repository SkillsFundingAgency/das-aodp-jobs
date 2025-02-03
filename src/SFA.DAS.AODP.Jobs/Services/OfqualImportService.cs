using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Interfaces;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualImportService : IOfqualImportService
    {
        private readonly ILogger<OfqualImportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterApi _apiClient;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IQualificationsService _qualificationsService;

        public OfqualImportService(ILogger<OfqualImportService> logger, IConfiguration configuration, IApplicationDbContext applicationDbContext, 
            IOfqualRegisterApi apiClient, IOfqualRegisterService ofqualRegisterService, IQualificationsService qualificationsService)
        {
            _logger = logger;
            _configuration = configuration;
            _applicationDbContext = applicationDbContext;
            _apiClient = apiClient;
            _ofqualRegisterService = ofqualRegisterService;
            _qualificationsService = qualificationsService;
        }

        public async Task StageQualificationsDataAsync(HttpRequestData request) 
        {
            _logger.LogInformation($"[{nameof(OfqualImportService)}] -> [{nameof(StageQualificationsDataAsync)}] -> Starting import Ofqual Qualifications to staging area...");

            int totalProcessed = 0;
            int pageCount = 1;
            var loopCycleStopWatch = new Stopwatch();
            var processStopWatch = new Stopwatch();
            processStopWatch.Start();

            try
            {
                _logger.LogInformation($"Clearing down StageQualifications table...");

                await _applicationDbContext.DeleteTable<StagedQualifications>();

                var parameters = _ofqualRegisterService.ParseQueryParameters(request.Query);

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

                    loopCycleStopWatch.Stop();
                    _logger.LogInformation($"Page {pageCount} import complete. {paginatedResult.Results.Count()} records imported in {loopCycleStopWatch.Elapsed.TotalSeconds:F2} seconds");

                    pageCount++;
                }

                processStopWatch.Stop();


                _logger.LogInformation($"Successfully imported {totalProcessed} qualifications in {processStopWatch.Elapsed.TotalSeconds:F2} seconds");
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

            using var transaction = await (_applicationDbContext as ApplicationDbContext)!.Database.BeginTransactionAsync();

            try
            {
                var importedQualifications = await _qualificationsService.GetStagedQualifcationsAsync();

                foreach (var qualificationData in importedQualifications)
                {
                    // Check for new Organisations
                    var organisation = _applicationDbContext.Organisation.Local
                        .FirstOrDefault(o => o.Name == qualificationData.OrganisationName);

                    if (organisation == null)
                    {
                        organisation = new Organisation
                        {
                            RecognitionNumber = qualificationData.OrganisationRecognitionNumber,
                            Name = qualificationData.OrganisationName,
                            Acronym = qualificationData.OrganisationAcronym
                        };
                        await _applicationDbContext.Organisation.AddAsync(organisation);
                    }

                    // Check for new Qualifications
                    var qualification = _applicationDbContext.Qualification.Local
                        .FirstOrDefault(o => o.Qan == qualificationData.QualificationNumber);

                    if (qualification == null)
                    {
                        qualification = new Qualification
                        {
                            Qan = qualificationData.QualificationNumberNoObliques,
                            QualificationName = qualificationData.Title
                        };
                        await _applicationDbContext.Qualification.AddAsync(qualification);
                    }

                    // Check for new QualificationVersion
                    var qualificationVersion = new QualificationVersion
                    {
                        QualificationId = qualification.Id,


                        OrganisationId = organisation.Id,
                        Status = qualificationData.Status,
                        Type = qualificationData.Type,
                        Level = qualificationData.Level,
                        RegulationStartDate = qualificationData.RegulationStartDate,
                        OperationalStartDate = qualificationData.OperationalStartDate,
                        LastUpdatedDate = DateTime.UtcNow,

                        Organisation = organisation,
                        Qualification = qualification
                    };

                    await _applicationDbContext.QualificationVersions.AddAsync(qualificationVersion);
                }

                await _applicationDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing qualifications.");
                throw;
            }
        }

    }
}
