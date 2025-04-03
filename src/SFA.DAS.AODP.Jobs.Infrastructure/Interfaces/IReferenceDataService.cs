namespace SFA.DAS.AODP.Infrastructure.Interfaces
{
    public interface IReferenceDataService
    {
        Guid GetActionTypeId(string actionType);
        Guid GetProcessStatusId(string processStatus);
        Guid GetLifecycleStageId(string stage);
    }
}
