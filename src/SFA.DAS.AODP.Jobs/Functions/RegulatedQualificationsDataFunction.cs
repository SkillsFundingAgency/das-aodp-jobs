using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Functions.Functions
{
    public class RegulatedQualificationsDataFunction
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILogger<RegulatedQualificationsDataFunction> _logger;
        private readonly IQualificationsService _qualificationsService;
        private readonly IOfqualImportService _ofqualImportService;


        public RegulatedQualificationsDataFunction(
            ILogger<RegulatedQualificationsDataFunction> logger, 
            IApplicationDbContext appDbContext, 
            IQualificationsService qualificationsService,
            IOfqualImportService ofqualImportService
            )
        {
            _logger = logger;
            _applicationDbContext = appDbContext;
            _qualificationsService = qualificationsService;
            _ofqualImportService = ofqualImportService;
        }

        [Function("RegulatedQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/regulatedQualificationsImport")] HttpRequestData req)
        {
            _logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Processing request...");

            var stopWatch = new Stopwatch();
            
            try
            {
                stopWatch.Start();

                // STAGE 1 - Import Ofqual Api data to staging area
                await _ofqualImportService.StageQualificationsDataAsync(req);

                // STAGE 2 - Process staging data into AODP database
                await _ofqualImportService.ProcessQualificationsDataAsync();

                stopWatch.Stop();

                _logger.LogInformation($"RegulatedQualificationsDataFunction completed in {stopWatch.Elapsed.TotalSeconds:F2} seconds");

                return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Successfully Imported Ofqual Data.");
            }
            catch (ApiException ex)
            {
                _logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected api exception occurred: {ex.Message}");
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (SystemException ex)
            {
                _logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected system exception occurred: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}