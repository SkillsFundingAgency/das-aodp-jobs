using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Functions
{
    public class FundedQualificationsDataFunction
    {
        private readonly ILogger<FundedQualificationsDataFunction> _logger;
        private readonly ICsvReaderService _csvReaderService;
        private readonly IMapper _mapper;
		private readonly AodpJobsConfiguration _config;
        private readonly IJobConfigurationService _jobConfigurationService;
        private readonly IFundedQualificationWriter _fundedQualificationWriter;
        private readonly IQualificationsRepository _qualificationsRepository;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger,            
            ICsvReaderService csvReaderService, 
            IMapper mapper, 
            AodpJobsConfiguration config, 
            IJobConfigurationService jobConfigurationService, 
            IFundedQualificationWriter fundedQualificationWriter,
            IQualificationsRepository qualificationsRepository)
        {
            _logger = logger;        
            _csvReaderService = csvReaderService;
            _mapper = mapper;          
            _config = config;
            _jobConfigurationService = jobConfigurationService;
            _fundedQualificationWriter = fundedQualificationWriter;
            _qualificationsRepository = qualificationsRepository;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport/{username}")] HttpRequestData req, string username = "")
        {
			string? fundedUrlFilePath = _config.FundedQualificationsImportUrl;
			string? archivedUrlFilePath = _config.ArchivedFundedQualificationsImportUrl;      

            if (string.IsNullOrEmpty(fundedUrlFilePath))
            {
                var errorMsg = "Config for 'FundedQualificationsImportUrl' is not set or empty.";
                _logger.LogError(errorMsg);
                return new BadRequestObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {errorMsg}");
            }

            if (string.IsNullOrEmpty(archivedUrlFilePath))
            {
                var errorMsg = "Config for 'ArchivedFundedQualificationsImportUrl' is not set or empty.";
                _logger.LogError(errorMsg);
                return new BadRequestObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {errorMsg}");
            }
            
            _logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Reading Configuration");
            var jobControl = await _jobConfigurationService.ReadFundedJobConfiguration();

            if (!jobControl.JobEnabled)
            {
                return new OkObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> Job disabled");
            }

            if (jobControl.Status == JobStatus.Running.ToString())
            {
                return new OkObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> Job currently running");
            }

            try
            {
                _logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Starting Job");
                var lastJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.FundedQualifications.ToString());
                if (lastJobRun.Id != Guid.Empty && lastJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    jobControl.JobRunId = lastJobRun.Id;
                    await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Running);
                }
                else
                {
                    jobControl.JobRunId = await _jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);
                }

                var qualifications = await _qualificationsRepository.GetQualificationsAsync();              
                var organisations = await _qualificationsRepository.GetAwardingOrganisationsAsync();
                
                var totalRecords = 0;
                var totalArchivedRecords = 0;

                var tablesCleared = false;
                if (jobControl.ImportFundedCsv)
                {
                    _logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Importing Funded CSV");
                    var approvedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(fundedUrlFilePath, qualifications, organisations, _logger);
                    //Commented out method to read a file from disk, useful for testing
                    //var path = "D:\\Source\\Repos\\das-aodp-jobs\\src\\SFA.DAS.AODP.Jobs\\Data\\approved.csv";
                    //var approvedQualifications = _csvReaderService.ReadCSVFromFilePath<FundedQualificationDTO, FundedQualificationsImportClassMap>(path, qualifications, organisations, _logger);

                    if (approvedQualifications.Any())
                    {
                        await _qualificationsRepository.TruncateFundingTables();
                        tablesCleared = true;
                        await _fundedQualificationWriter.WriteQualifications(approvedQualifications);                        
                    }
                    else
                    {
                        var warningMsg = "No data found found in approved qualifications csv";
                        _logger.LogWarning(warningMsg);
                        await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                        return new NotFoundObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {warningMsg}");
                    }
                    totalRecords = approvedQualifications.Count();
                }

                if (jobControl.ImportArchivedCsv)
                {
                    _logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Importing Archived CSV");
                    var archivedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(archivedUrlFilePath, qualifications, organisations, _logger);
                    if (archivedQualifications.Any())
                    {
                        if (!tablesCleared)
                        {
                            await _qualificationsRepository.TruncateFundingTables();
                        }
                        await _fundedQualificationWriter.WriteQualifications(archivedQualifications);
                    }
                    else
                    {
                        var warningMsg = "No data found found in archived qualifications csv";
                        _logger.LogWarning(warningMsg);
                        await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                        return new NotFoundObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {warningMsg}");
                    }

                    totalArchivedRecords = archivedQualifications.Count();
                    _logger.LogInformation($"{totalArchivedRecords} records imported");
                }
                
                if ((totalRecords + totalArchivedRecords) > 0)
                {
                    _logger.LogInformation($"Seeding funded data into funding offers");
                    await _fundedQualificationWriter.SeedFundingData();
                }

                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Completed);

                var msg = $"[{nameof(FundedQualificationsDataFunction)}] -> {totalRecords} approved qualifications imported, {totalArchivedRecords} archived qualifications imported";
                _logger.LogInformation(msg);
                return new OkObjectResult(msg);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"[{nameof(FundedQualificationsDataFunction)}] -> Unexpected api exception occurred: {ex.Message}");
                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (SystemException ex)
            {
                _logger.LogError(ex, $"[{nameof(FundedQualificationsDataFunction)}] -> Unexpected system exception occurred: {ex.Message}");
                await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                return new StatusCodeResult(500);
            }
        }
    }
}