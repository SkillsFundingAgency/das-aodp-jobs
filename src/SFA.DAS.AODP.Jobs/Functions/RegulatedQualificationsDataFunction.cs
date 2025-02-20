using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using System.Diagnostics;

namespace SFA.DAS.AODP.Functions.Functions
{
    public class RegulatedQualificationsDataFunction
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILogger<RegulatedQualificationsDataFunction> _logger;
        private readonly IQualificationsService _qualificationsService;
        private readonly IOfqualImportService _ofqualImportService;
        private readonly IJobConfigurationService _jobConfigurationService;

        public RegulatedQualificationsDataFunction(
            ILogger<RegulatedQualificationsDataFunction> logger, 
            IApplicationDbContext appDbContext, 
            IQualificationsService qualificationsService,
            IOfqualImportService ofqualImportService,
            IJobConfigurationService jobConfigurationService
            )
        {
            _logger = logger;
            _applicationDbContext = appDbContext;
            _qualificationsService = qualificationsService;
            _ofqualImportService = ofqualImportService;
            _jobConfigurationService = jobConfigurationService;          
        }

        [Function("RegulatedQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/regulatedQualificationsImport/{username}")] HttpRequestData req, string username = "")
        {
            _logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Processing request by user {username}");

            var stopWatch = new Stopwatch();

            _logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Reading Configuration");
            var jobControl = await _jobConfigurationService.ReadJobConfiguration();           
            var totalRecords = 0;

            if (!jobControl.JobEnabled)
            {
                return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Job disabled");
            }            

            _logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Configuration set to Run Api Import = {jobControl.RunApiImport}, Process Staging Data = {jobControl.ProcessStagingData}");

            try
            {
                stopWatch.Start();

                jobControl.JobRunId = await _jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);

                if (jobControl.RunApiImport)
                {
                    // STAGE 1 - Import Ofqual Api data to staging area
                    totalRecords = await _ofqualImportService.ImportApiData(req);
                }

                if (jobControl.ProcessStagingData)
                {
                    // STAGE 2 - Process staging data into AODP database
                    await _ofqualImportService.ProcessQualificationsDataAsync();
                }

                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Completed);

                stopWatch.Stop();

                _logger.LogInformation($"RegulatedQualificationsDataFunction completed in {stopWatch.Elapsed.TotalSeconds:F2} seconds");

                return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Successfully Imported Ofqual Data.");
            }
            catch (ApiException ex)
            {
                _logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected api exception occurred: {ex.Message}");
                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Error);
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (SystemException ex)
            {
                _logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected system exception occurred: {ex.Message}");
                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Error);
                return new StatusCodeResult(500);
            }
        }        
    }
}