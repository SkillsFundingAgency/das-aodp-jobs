using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.AODP.Models.Qualification;
using System.Diagnostics;

namespace SFA.DAS.AODP.Functions
{
    public class FundedQualificationsDataFunction
    {
        private readonly ILogger<FundedQualificationsDataFunction> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ICsvReaderService _csvReaderService;
        private readonly IMapper _mapper;
		private readonly AodpJobsConfiguration _config;
        private readonly IJobConfigurationService _jobConfigurationService;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService, 
            IMapper mapper, AodpJobsConfiguration config, IJobConfigurationService jobConfigurationService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
            _mapper = mapper;          
            _config = config;
            _jobConfigurationService = jobConfigurationService;
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
                var lastJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString());
                if (lastJobRun.Id != Guid.Empty && lastJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    jobControl.JobRunId = lastJobRun.Id;
                    await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Running);
                }
                else
                {
                    jobControl.JobRunId = await _jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);
                }

                var qualifications = await _applicationDbContext.Qualification
                    .AsNoTracking()
                    .ToListAsync();

                // order organisations by recognition number desc and select by highest recoginition number
                var organisations = await _applicationDbContext.AwardingOrganisation
                    .AsNoTracking()
                    .OrderByDescending(o => o.RecognitionNumber)
                    .GroupBy(o => o.NameOfqual)
                    .Select(g => g.First())
                    .ToListAsync();

                var stopWatch = new Stopwatch();
                var totalRecords = 0;
                var totalArchivedRecords = 0;

                if (jobControl.ImportFundedCsv)
                {
                    _logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Importing Funded CSV");
                    var approvedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(fundedUrlFilePath, qualifications, organisations, _logger);

                    if (approvedQualifications.Any())
                    {
                        await _applicationDbContext.Truncate_FundedQualifications();

                        await WriteQualifications(approvedQualifications, stopWatch);
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
                        await WriteQualifications(archivedQualifications, stopWatch);
                    }
                    else
                    {
                        var warningMsg = "No data found found in archived qualifications csv";
                        _logger.LogWarning(warningMsg);
                        await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                        return new NotFoundObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {warningMsg}");
                    }

                    totalArchivedRecords = archivedQualifications.Count();
                    _logger.LogInformation($"{totalArchivedRecords} records imported in {stopWatch.ElapsedMilliseconds / 1000}");
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

        private async Task WriteQualifications(List<FundedQualificationDTO> qualifications, Stopwatch stopWatch)
        {
            stopWatch.Restart();

            const int _batchSize = 1000;

            for (int i = 0; i < qualifications.Count; i += _batchSize)
            {
                var batch = qualifications
                    .Skip(i)
                    .Take(_batchSize)
                    .ToList();

                var entities = _mapper.Map<List<Qualifications>>(batch);

                await _applicationDbContext.FundedQualifications.AddRangeAsync(entities);
            }

            await _applicationDbContext.SaveChangesAsync();

            stopWatch.Stop();
        }
    }
}