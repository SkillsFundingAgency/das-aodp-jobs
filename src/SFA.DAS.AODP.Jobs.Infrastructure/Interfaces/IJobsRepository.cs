using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Interfaces
{
    public interface IJobsRepository
    {
        Task<Job?> GetJobByIdAsync(Guid id);
        Task<Job?> GetJobByNameAsync(string name);
        Task<List<JobConfiguration>> GetJobConfigurationsByIdAsync(Guid jobId);
        Task<JobRun?> GetJobRunByIdAsync(Guid id);
        Task<JobRun?> GetLastJobRunsAsync(string jobName);
        Task<List<Job>> GetJobsAsync();
        Task<Guid> InsertJobRunAsync(Guid jobId, string user, DateTime startTime, string status);
        Task<bool> UpdateJobAsync(Guid id, DateTime lastRunTime, string status);
        Task<bool> UpdateJobRunAsync(Guid id, string user, DateTime stopTime, string status, int recordsProcessed);
    }
}