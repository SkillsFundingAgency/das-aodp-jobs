namespace SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services
{
    public interface ISystemClockService
    {
        /// <summary>Retrieves the current system time in UTC.</summary>
        DateTime UtcNow { get; }

        DateOnly Today { get; }
    }

    public class SystemClockService : ISystemClockService
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
