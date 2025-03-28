using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ISchedulerClientService
    {
        Task ExecuteFunction(JobRunControl requestedJobRun, string functionName, string functionUrlPartial);
    }
}