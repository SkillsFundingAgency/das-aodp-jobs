using System.Diagnostics;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Functions.Functions
{
    public class RegulatedQualificationsDataFunction
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILogger<RegulatedQualificationsDataFunction> _logger;
        private readonly IQualificationsService _qualificationsService;
        private readonly IOfqualRegisterService _ofqualRegisterService;

        public RegulatedQualificationsDataFunction(
            ILogger<RegulatedQualificationsDataFunction> logger, 
            IApplicationDbContext appDbContext, 
            IQualificationsService qualificationsService,
            IOfqualRegisterService ofqualRegisterService,
            IMapper mapper)
        {
            _logger = logger;
            _applicationDbContext = appDbContext;
            _qualificationsService = qualificationsService;
            _ofqualRegisterService = ofqualRegisterService;
        }

        [Function("RegulatedQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/regulatedQualificationsImport")] HttpRequestData req)
        {
            _logger.LogInformation($"Processing {nameof(RegulatedQualificationsDataFunction)} request...");

            try
            {
                int totalProcessed = 0;
                int pageCount = 1;
                var loopCycleStopWatch = new Stopwatch();
                var processStopWatch = new Stopwatch();
                processStopWatch.Start();

                var processedQualifications = await _qualificationsService.GetAllProcessedRegulatedQualificationsAsync();

                //_logger.LogInformation($"Clearing down RegulatedQualificationsImport table...");
                //await _applicationDbContext.DeleteTable<RegulatedQualificationsImport>();

                _logger.LogInformation($"Clearing down RegulatedQualificationsImportStaging table...");
                await _applicationDbContext.DeleteTable<RegulatedQualificationsImportStaging>();

                var parameters = _ofqualRegisterService.ParseQueryParameters(req.Query);

                while (true)
                {
                    loopCycleStopWatch.Reset();
                    loopCycleStopWatch.Start();

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

                    await _qualificationsService.SaveRegulatedQualificationsStagingAsync(importedQualificationsJson);

                    //var importedQualifications = _ofqualRegisterService.ExtractQualificationsList(paginatedResult);
                    //await _qualificationsService.CompareAndUpdateQualificationsAsync(importedQualifications, processedQualifications);
                    //await _qualificationsService.SaveRegulatedQualificationsAsync(importedQualifications);

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

                _logger.LogInformation($"Total qualifications processed: {totalProcessed} in {processStopWatch.Elapsed.TotalSeconds:F2} seconds");
                return new OkObjectResult($"Successfully processed {totalProcessed} qualifications.");
            }
            catch (ApiException ex)
            {
                _logger.LogError($"Unexpected api exception occurred: {ex.Message}");
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (SystemException ex)
            {
                _logger.LogError($"Unexpected system exception occurred: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}