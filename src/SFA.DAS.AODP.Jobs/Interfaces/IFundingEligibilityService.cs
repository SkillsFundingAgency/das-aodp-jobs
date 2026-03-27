using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IFundingEligibilityService
    {
        public FundingEligibilityEvaluation EvaluateFundingEligibilityRules(
            QualificationDTO qualification);
        public FundingEligibilityComparison CompareEligibilityRules(
            QualificationDTO previousQualification, QualificationDTO currentQualification);
    }
}
