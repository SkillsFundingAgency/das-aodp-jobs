using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;
using System.Linq;

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
                _logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} eligible for funding");
            }
            else
            {
                _logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} NOT eligible for funding");
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
                _logger.LogInformation($"[{nameof(FundingEligibilityService)}] -> [{nameof(EligibleForFunding)}] -> Qualification {qualification.QualificationNumberNoObliques} has no GLH/TQT");
                reason = ImportReason.NoGLHOrTQT;
            }            

            return reason;
        }
    }
}
