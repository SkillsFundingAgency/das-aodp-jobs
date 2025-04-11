using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ISchedulerClientService
    {
        Task<bool> ExecuteFunction(JobRunControl requestedJobRun, string functionName, string functionUrlPartial);
    }
}