namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IQaaQualificationSeedService
{
    Task<int> SeedAsync(CancellationToken cancellationToken);
}
