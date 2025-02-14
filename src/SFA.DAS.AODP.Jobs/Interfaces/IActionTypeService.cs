using SFA.DAS.AODP.Jobs.Enum;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IActionTypeService
    {
        Guid GetActionTypeId(ActionTypeEnum actionType);
    }
}
