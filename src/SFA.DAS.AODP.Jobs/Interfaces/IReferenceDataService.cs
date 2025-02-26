using SFA.DAS.AODP.Jobs.Enum;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IReferenceDataService
    {
        Guid GetActionTypeId(string actionType);
        Guid GetProcessStatusId(string processStatus);
        Guid GetLifecycleStageId(string stage);
    }
}
