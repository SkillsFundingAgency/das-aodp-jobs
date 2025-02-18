using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.Enum;
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

        public async Task<JobControl> ReadJobConfiguration()
        {
            var jobControl = new JobControl();
            var jobRecord = await _jobsRepository.GetJobByNameAsync(JobNames.RegulatedQualifications.ToString());
            jobControl.JobEnabled = jobRecord?.Enabled ?? false;
            jobControl.JobId = jobRecord?.Id ?? Guid.Empty;
            jobControl.RunApiImport = false;
            jobControl.ProcessStagingData = false;

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

        public async Task<Guid> InsertJobRunAsync(Guid jobId, string userName, JobStatus status)
        {
            var startTime = _systemClockService.UtcNow;
            return await _jobsRepository.InsertJobRunAsync(jobId, userName, startTime, status.ToString());
        }
    }
    public struct JobControl
    {
        public Guid JobId;
        public Guid JobRunId;
        public bool RunApiImport;
        public bool ProcessStagingData;
        public bool JobEnabled;
    }
}
