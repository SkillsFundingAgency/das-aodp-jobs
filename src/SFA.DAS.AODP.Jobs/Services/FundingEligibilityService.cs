namespace SFA.DAS.AODP.Jobs.Services;

public class FundingEligibilityService(ILogger<FundingEligibilityService> logger) : IFundingEligibilityService
{
    public bool EligibleForFunding(QualificationDTO qualification)
    {            
        var eligibleForFunding = qualification.OfferedInEngland
                                 && qualification.Type != QualificationReference.EndPointAssessment                                     
                                 && !QualificationReference.IneligibleQualifications.Any(s => qualification.Title.Contains(s, StringComparison.OrdinalIgnoreCase))
                                 && !QualificationReference.IneligibleQualificationsShortForms.Any(s => qualification.Title.Contains(s, StringComparison.OrdinalIgnoreCase))
                                 && qualification.Glh.HasValue && qualification.Tqt.HasValue
                                 && qualification.Glh.Value > 0 && qualification.Tqt.Value > 0
                                 && qualification.Glh < qualification.Tqt
                                 && qualification.OperationalStartDate >= QualificationReference.MinOperationalDate;

        if (eligibleForFunding)
        {
            logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} eligible for funding");
        }
        else
        {
            logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} NOT eligible for funding");
        }

        return eligibleForFunding;
    }

    public string DetermineFailureReason(QualificationDTO qualification)
    {
        var reason = ImportReason.NoAction;

        var noGlhOrTqt = !qualification.Glh.HasValue 
                         || !qualification.Tqt.HasValue
                         || (qualification.Glh.Value <= 0 && qualification.Tqt.Value <= 0);

        if (noGlhOrTqt)
        {
            logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} has no GLH/TQT");
            reason = ImportReason.NoGLHOrTQT;
        }            

        return reason;
    }
}