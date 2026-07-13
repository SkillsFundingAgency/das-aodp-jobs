using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services;

public class FundingEligibilityService : IFundingEligibilityService
{
    private readonly ILogger<FundingEligibilityService> _logger;

    public FundingEligibilityService(ILogger<FundingEligibilityService> logger)
    {
        _logger = logger;
    }

    public bool EligibleForFunding(QualificationDTO qualification)
    {
        return qualification.OfferedInEngland
               && (qualification.IntentionToSeekFundingInEngland ?? false)
               && !QualificationReference.IsIneligibleType(qualification.Type)
               && !QualificationReference.HasIneligibleTitle(qualification.Level, qualification.Title);
    }

    public string DetermineFailureReason(QualificationDTO qualification) => ImportReason.NoAction;
}