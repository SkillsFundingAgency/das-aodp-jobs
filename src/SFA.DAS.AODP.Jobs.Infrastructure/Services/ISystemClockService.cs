using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services
{
    public interface ISystemClockService
    {
        /// <summary>Retrieves the current system time in UTC.</summary>
        DateTime UtcNow { get; }
    }

    public class SystemClockService : ISystemClockService
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
