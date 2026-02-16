using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Jobs.Models.Jobs;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class JobConfigurationService : IJobConfigurationService
    {
        private readonly IJobsRepository _jobsRepository;
        private readonly ISystemClockService _systemClockService;

        public JobConfigurationService(IJobsRepository jobsRepository,
            ISystemClockService systemClockService)
        {
            _jobsRepository = jobsRepository;
            _systemClockService = systemClockService;
        }

        public async Task UpdateJobRun(string username, Guid jobId, Guid jobRunId, int totalRecords, JobStatus status)
        {
            var finishTime = _systemClockService.UtcNow;
            if (jobRunId != Guid.Empty)
            {
                var jobRunUpdateOk = await _jobsRepository.UpdateJobRunAsync(jobRunId, username, finishTime, status.ToString(), totalRecords);
            }
            if (jobId != Guid.Empty)
            {
                await _jobsRepository.UpdateJobAsync(jobId, finishTime, status.ToString());
            }
        }

        public async Task<JobControl> ReadJobConfiguration(JobNames jobName)
        {
            return jobName switch
            {
                JobNames.RegulatedQualifications => await ReadRegulatedJobConfiguration(),
                JobNames.FundedQualifications => await ReadFundedJobConfiguration(),
                JobNames.Pldns => await ReadPldnsImportConfiguration(),
                JobNames.DefundingList => await ReadDefundingListImportConfiguration(),
                JobNames.QaaQualifications => await ReadQaaQualificationJobConfiguration(),
                _ => throw new ArgumentOutOfRangeException(nameof(jobName), jobName, null)
            };
        }

        public async Task<QaaQualificationJobControl> ReadQaaQualificationJobConfiguration()
        {
            var jobControl = new QaaQualificationJobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(nameof(JobNames.QaaQualifications));
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.Status = jobRecord?.Status ?? string.Empty;

            if (jobControl.JobId != Guid.Empty)
            {
                var runApiImport = jobRecord!.JobConfigurations.FirstOrDefault(o => o.Name == nameof(JobConfiguration.ImportQaaQualifications))?.Value ?? "false";
                jobControl.RunApiImport = bool.Parse(runApiImport);
            }

            return jobControl;
        }

        public async Task<RegulatedJobControl> ReadRegulatedJobConfiguration()
        {
            var jobControl = new RegulatedJobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(nameof(JobNames.RegulatedQualifications));
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.RunApiImport = false;
            jobControl.ProcessStagingData = false;
            jobControl.Status = jobRecord?.Status ?? string.Empty;

            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var runApiImportValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ApiImport))?.Value ?? "false";
                var processStagingDataValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ProcessStagingData))?.Value ?? "false";
                
                jobControl.RunApiImport = bool.Parse(runApiImportValue);
                jobControl.ProcessStagingData = bool.Parse(processStagingDataValue);
            }

            return jobControl;
        }

        public async Task<FundedJobControl> ReadFundedJobConfiguration()
        {
            var jobControl = new FundedJobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(nameof(JobNames.FundedQualifications));
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.ImportFundedCsv = false;
            jobControl.ImportArchivedCsv = false;
            jobControl.Status = jobRecord?.Status ?? string.Empty;

            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importFundedCsvValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ImportFundedCsv))?.Value ?? "false";
                var importArchivedCsvValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ImportArchivedCsv))?.Value ?? "false";
                
                jobControl.ImportFundedCsv = bool.Parse(importFundedCsvValue);
                jobControl.ImportArchivedCsv = bool.Parse(importArchivedCsvValue);
            }

            return jobControl;
        }

        public async Task<Guid> InsertJobRunAsync(Guid jobId, string userName, JobStatus status)
        {
            var startTime = _systemClockService.UtcNow;
            return await _jobsRepository.InsertJobRunAsync(jobId, userName, startTime, status.ToString());
        }

        public async Task<JobRunControl> GetLastJobRunAsync(string jobName)
        {
            var jobRunRecord = await _jobsRepository.GetLastJobRunsAsync(jobName);
            var jobRunControl = new JobRunControl()
            {
                Id = jobRunRecord?.Id ?? Guid.Empty,
                JobId = jobRunRecord?.JobId ?? Guid.Empty,
                Status = jobRunRecord?.Status ?? string.Empty,
                StartTime = jobRunRecord?.StartTime ?? DateTime.MinValue,
                EndTime = jobRunRecord?.EndTime ?? DateTime.MinValue,
                User = jobRunRecord?.User ?? string.Empty,
                RecordsProcessed = jobRunRecord?.RecordsProcessed ?? 0
            };

            return jobRunControl;
        }

        public async Task<PldnsImportControl> ReadPldnsImportConfiguration()
        {
            var jobControl = new PldnsImportControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(nameof(JobNames.Pldns));
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.Status = jobRecord?.Status ?? string.Empty;
            jobControl.JobRunId = jobRecord?.Id ?? Guid.Empty;
            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importPldnsValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ImportPldns))?.Value ?? "false";
                bool importPldnsParsed = bool.TryParse(importPldnsValue, out bool importPldns);
                jobControl.ImportPldns = importPldnsParsed && importPldns;
            }

            return jobControl;
        }

        public async Task<DefundingListImportControl> ReadDefundingListImportConfiguration()
        {
            var jobControl = new DefundingListImportControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(nameof(JobNames.DefundingList));
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.Status = jobRecord?.Status ?? string.Empty;
            jobControl.JobRunId = jobRecord?.Id ?? Guid.Empty;
            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importDefundingListValue = configEntries.FirstOrDefault(f => f.Name == nameof(JobConfiguration.ImportDefundingList))?.Value ?? "false";
                bool importDefundingListParsed = bool.TryParse(importDefundingListValue, out bool importDefundingList);
                jobControl.ImportDefundingList = importDefundingListParsed && importDefundingList;
            }

            return jobControl;
        }
    }
}