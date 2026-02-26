namespace SFA.DAS.AODP.Infrastructure.Services;

public interface ISystemClockService
{
    /// <summary>Retrieves the current system time in UTC.</summary>
    DateTime UtcNow { get; }
}

public class SystemClockService : ISystemClockService
{
    public DateTime UtcNow => DateTime.UtcNow;
}