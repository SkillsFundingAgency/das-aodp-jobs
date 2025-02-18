using SFA.DAS.AODP.Jobs.Enum;

namespace SFA.DAS.AODP.Jobs.Services
{
    public interface IJobConfigurationService
    {
        Task<JobControl> ReadJobConfiguration();
        Task UpdateJobRun(string username, Guid jobId, Guid jobRunId, int totalRecords, JobStatus status);
        Task<Guid> InsertJobRunAsync(Guid jobId, string userName, JobStatus status);
    }
}