using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Functions.Functions
{
    public class RegulatedQualificationsDataFunction
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILogger<RegulatedQualificationsDataFunction> _logger;
        private readonly IRegulatedQualificationsService _regulatedQualificationsService;
        private readonly IOfqualRegisterService _ofqualRegisterService;
        private readonly IMapper _mapper;

        public RegulatedQualificationsDataFunction(
            ILogger<RegulatedQualificationsDataFunction> logger, 
            IApplicationDbContext appDbContext, 
            IRegulatedQualificationsService regulatedQualificationsService,
            IOfqualRegisterService ofqualRegisterService,
            IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
            _applicationDbContext = appDbContext;
            _regulatedQualificationsService = regulatedQualificationsService;
            _ofqualRegisterService = ofqualRegisterService;
        }

        [Function("RegulatedQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/regulatedQualificationsImport")] HttpRequestData req)
        {
            _logger.LogInformation($"Processing {nameof(RegulatedQualificationsDataFunction)} request...");

            try
            {
                int page = 1;
                int limit = 100;
                int totalProcessed = 0;

                var processedQualificationsEntities = await _applicationDbContext.ProcessedRegulatedQualifications.ToListAsync();
                var processedQualifications = _mapper.Map<List<RegulatedQualificationDTO>>(processedQualificationsEntities);

                var parameters = _ofqualRegisterService.ParseQueryParameters(req.Query);

                while (true)
                {
                    var paginatedResult = await _ofqualRegisterService.SearchPrivateQualificationsAsync(parameters, page, limit);

                if (paginatedResult.Results == null || !paginatedResult.Results.Any())
                {
                    _logger.LogInformation("No more qualifications to process.");
                    break;
                }

                _logger.LogInformation($"Processing page {page}. Retrieved {paginatedResult.Results.Count} qualifications.");

                    List<RegulatedQualificationDTO> importedQualifications = _ofqualRegisterService.ExtractQualificationsList(paginatedResult);

                    await _regulatedQualificationsService.CompareAndUpdateQualificationsAsync(importedQualifications, processedQualifications);

                    await _regulatedQualificationsService.SaveRegulatedQualificationsAsync(importedQualifications);

                    totalProcessed += importedQualifications.Count;

                    if (paginatedResult.Results.Count < limit)
                    {
                        _logger.LogInformation("Reached the end of the results set.");
                        break;
                    }

                    page++;
                }

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
