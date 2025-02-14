using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IFundingEligibilityService
    {
        public bool EligibleForFunding(QualificationDTO qualification);
    }
}
