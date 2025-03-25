using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IJobConfigurationService
    {
        Task<RegulatedJobControl> ReadRegulatedJobConfiguration();
        Task<FundedJobControl> ReadFundedJobConfiguration();
        Task UpdateJobRun(string username, Guid jobId, Guid jobRunId, int totalRecords, JobStatus status);
        Task<Guid> InsertJobRunAsync(Guid jobId, string userName, JobStatus status);
        Task<JobRunControl> GetLastJobRunAsync(string jobName);
    }
}