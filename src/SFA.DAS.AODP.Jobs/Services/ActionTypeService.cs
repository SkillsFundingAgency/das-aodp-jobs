using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class ActionTypeService : IActionTypeService
    {
        private readonly ILogger<ActionTypeService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly Dictionary<ActionTypeEnum, Guid> _actionTypeMap;

        public ActionTypeService(ILogger<ActionTypeService> logger, IApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _actionTypeMap = _applicationDbContext.ActionType
                .ToDictionary(a => MapToEnum(a.Description), a => a.Id);
        }

        public Guid GetActionTypeId(ActionTypeEnum actionType)
        {
            _logger.LogInformation($"[{nameof(ActionTypeService)}] -> [{nameof(GetActionTypeId)}] -> Retrieving action type id for action type {actionType}...");

            return _actionTypeMap.TryGetValue(actionType, out var id)
                ? id
                : throw new KeyNotFoundException($"ActionTypeEnum {actionType} not found in the database.");
        }

        private static ActionTypeEnum MapToEnum(string description)
        {
            return description switch
            {
                "No Action Required" => ActionTypeEnum.NoActionRequired,
                "Action Required" => ActionTypeEnum.ActionRequired,
                "Ignore" => ActionTypeEnum.Ignore,
                _ => throw new ArgumentException("Invalid action type description")
            };
        }

    }
}
