using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class FundingEligibilityService : IFundingEligibilityService
    {
        private readonly ILogger<FundingEligibilityService> _logger;

        public FundingEligibilityService(ILogger<FundingEligibilityService> logger)
        {
            _logger = logger;
        }

        public bool EligibleForFunding(QualificationDTO qualification)
        {
            _logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Running eligibility test for qualifcation with qan {qualification.QualificationNumberNoObliques}...");

            return false;
        }
    }
}
