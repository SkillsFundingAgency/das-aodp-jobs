using System.Diagnostics;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RestEase;
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
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                int page = 1;
                int limit = 1000;
                int totalProcessed = 0;

                var processedQualifications = await _qualificationsService.GetAllProcessedRegulatedQualificationsAsync();

                _logger.LogInformation($"Clearing down RegulatedQualificationsImport table...");

                await _applicationDbContext.DeleteFromTable("RegulatedQualificationsImport");

                var parameters = _ofqualRegisterService.ParseQueryParameters(req.Query);

                while (true)
                {
                    var paginatedResult = await _ofqualRegisterService.SearchPrivateQualificationsAsync(parameters, page, limit);

                    if (paginatedResult.Results == null || !paginatedResult.Results.Any())
                    {
                        _logger.LogInformation("No more qualifications to process.");
                        break;
                    }

                    _logger.LogInformation($"Processing page {page}. Retrieved {paginatedResult.Results?.Count} qualifications.");

                    var importedQualifications = _ofqualRegisterService.ExtractQualificationsList(paginatedResult);

                    await _qualificationsService.CompareAndUpdateQualificationsAsync(importedQualifications, processedQualifications);

                    await _qualificationsService.SaveRegulatedQualificationsAsync(importedQualifications);

                    totalProcessed += importedQualifications.Count;

                    if (paginatedResult.Results?.Count < limit)
                    {
                        _logger.LogInformation("Reached the end of the results set.");
                        break;
                    }

                    page++;

                    _logger.LogInformation($"{importedQualifications.Count()} records imported in {stopWatch.ElapsedMilliseconds / 1000}");
                }

                stopWatch.Stop();

                _logger.LogInformation($"Total qualifications processed: {totalProcessed}");
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