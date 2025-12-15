using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

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

        public async Task<RegulatedJobControl> ReadRegulatedJobConfiguration()
        {
            var jobControl = new RegulatedJobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(JobNames.RegulatedQualifications.ToString());
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.RunApiImport = false;
            jobControl.ProcessStagingData = false;
            jobControl.Status = jobRecord.Status;

            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var runApiImportValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ApiImport.ToString())?.Value ?? "false";
                bool.TryParse(runApiImportValue, out jobControl.RunApiImport);
                var processStagingDataValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ProcessStagingData.ToString())?.Value ?? "false";
                bool.TryParse(processStagingDataValue, out jobControl.ProcessStagingData);
            }

            return jobControl;
        }

        public async Task<FundedJobControl> ReadFundedJobConfiguration()
        {
            var jobControl = new FundedJobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(JobNames.FundedQualifications.ToString());
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.ImportFundedCsv = false;
            jobControl.ImportArchivedCsv = false;
            jobControl.Status = jobRecord.Status;

            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importFundedCsvValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ImportFundedCsv.ToString())?.Value ?? "false";
                bool.TryParse(importFundedCsvValue, out jobControl.ImportFundedCsv);
                var ImportArchivedCsvValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ImportArchivedCsv.ToString())?.Value ?? "false";
                bool.TryParse(ImportArchivedCsvValue, out jobControl.ImportArchivedCsv);
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
            var jobRecord = await _jobsRepository.GetJobByNameAsync(JobNames.Pldns.ToString());
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.Status = jobRecord.Status;
            jobControl.JobRunId = jobRecord?.Id ?? Guid.Empty;
            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importPldnsValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ImportPldns.ToString())?.Value ?? "false";
                bool.TryParse(importPldnsValue, out jobControl.ImportPldns);
            }

            return jobControl;
        }

        public async Task<DefundingListImportControl> ReadDefundingListImportConfiguration()
        {
            var jobControl = new DefundingListImportControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(JobNames.DefundingList.ToString());
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.Status = jobRecord.Status;
            jobControl.JobRunId = jobRecord?.Id ?? Guid.Empty;
            if (jobControl.JobId != Guid.Empty)
            {
                var configEntries = await _jobsRepository.GetJobConfigurationsByIdAsync(jobControl.JobId);
                var importDefundingListValue = configEntries.FirstOrDefault(f => f.Name == JobConfiguration.ImportDefundingList.ToString())?.Value ?? "false";
                bool.TryParse(importDefundingListValue, out jobControl.ImportDefundingList);
            }

            return jobControl;
        }
    }

    public class RegulatedJobControl
    {
        public Guid JobId;
        public Guid JobRunId;
        public bool RunApiImport;
        public bool ProcessStagingData;
        public bool JobEnabled;
        public string Status;
    }

    public class FundedJobControl
    {
        public Guid JobId;
        public Guid JobRunId;
        public bool ImportFundedCsv;
        public bool ImportArchivedCsv;
        public bool JobEnabled;
        public string Status;
    }

    public class PldnsImportControl
    {
        public Guid JobId;
        public Guid JobRunId;
        public bool ImportPldns;
        public bool JobEnabled;
        public string Status;
    }

    public class DefundingListImportControl
    {
        public Guid JobId;
        public Guid JobRunId;
        public bool ImportDefundingList;
        public bool JobEnabled;
        public string Status;
    }

    public class JobRunControl
    {
        public Guid Id;
        public string Status;
        public DateTime StartTime;
        public DateTime? EndTime;
        public string User;
        public int? RecordsProcessed;
        public Guid JobId;
    }
}
