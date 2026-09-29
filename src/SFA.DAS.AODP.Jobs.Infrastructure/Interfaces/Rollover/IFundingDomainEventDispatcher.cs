using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;

namespace SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;

public interface IFundingDomainEventDispatcher
{
    Task DispatchAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<FundingDomainEvent> events,
        CancellationToken cancellationToken);
}
