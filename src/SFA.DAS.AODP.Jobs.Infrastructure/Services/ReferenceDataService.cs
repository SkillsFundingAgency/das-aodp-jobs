using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;

namespace SFA.DAS.AODP.Infrastructure.Services;

public class ReferenceDataService : IReferenceDataService
{
    private readonly ILogger<ReferenceDataService> _logger;
    private readonly IApplicationDbContext _applicationDbContext;
    private Dictionary<string, Guid> _actionTypeMap = null!;
    private Dictionary<string, Guid> _processStatusMap = null!;
    private Dictionary<string, Guid> _lifecycleStageMap = null!;

    public ReferenceDataService(ILogger<ReferenceDataService> logger, IApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));

        Initialise();
    }

    public Guid GetActionTypeId(string actionType)
    {
        return _actionTypeMap.TryGetValue(actionType, out var id)
            ? id
            : throw new KeyNotFoundException($"ActionTypeEnum {actionType} not found in the database.");
    }

    public Guid GetProcessStatusId(string processStatus)
    {
        return _processStatusMap.TryGetValue(processStatus, out var id)
            ? id
            : throw new KeyNotFoundException($"ActionTypeEnum {processStatus} not found in the database.");
    }

    public Guid GetLifecycleStageId(string stage)
    {
        return _lifecycleStageMap.TryGetValue(stage, out var id)
            ? id
            : throw new KeyNotFoundException($"Lifecycle Stage {stage} not found in the database.");
    }

    private void Initialise()
    {
        _logger.LogInformation("Initialising reference data.");

        _actionTypeMap = _applicationDbContext.ActionType.ToDictionary(a => a.Description!, a => a.Id)!;
        _processStatusMap = _applicationDbContext.ProcessStatus.ToDictionary(a => a.Name!, a => a.Id)!;
        _lifecycleStageMap = _applicationDbContext.LifecycleStages.ToDictionary(a => a.Name!, a => a.Id)!;
    }
}