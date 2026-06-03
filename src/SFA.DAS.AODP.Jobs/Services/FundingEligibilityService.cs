using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services;

public class FundingEligibilityService : IFundingEligibilityService
{
    private readonly ILogger<FundingEligibilityService> _logger;

    public bool EligibleForFunding(QualificationDTO qualification)
    {
        return qualification.OfferedInEngland
               && (qualification.IntentionToSeekFundingInEngland ?? false)
               && !QualificationReference.IsIneligibleType(qualification.Type)
               && !QualificationReference.HasIneligibleTitle(qualification.Level, qualification.Title);
    }

    public FundingEligibilityEvaluation EvaluateFundingEligibilityRules(QualificationDTO qualification)
    {
        var rules = new List<FundingEligibilityRuleResult>
        {
            new FundingEligibilityRuleResult(
                "OfferedInEngland",
                qualification.OfferedInEngland,
                ["OfferedInEngland"]),

            new FundingEligibilityRuleResult(
                "IntentionToSeekFundingInEngland",
                qualification.IntentionToSeekFundingInEngland ?? false,
                ["IntentionToSeekFundingInEngland"]),

            new FundingEligibilityRuleResult(
                "Type",
                !QualificationReference.IsIneligibleType(qualification.Type),
                ["Type"]),

            new FundingEligibilityRuleResult(
                "Title",
                !QualificationReference.HasIneligibleTitle(qualification.Level, qualification.Title),
                ["Title"]),

        };

        return new FundingEligibilityEvaluation
        {
            Rules = rules
        };
    }


        public FundingEligibilityComparison CompareEligibilityRules(
            QualificationDTO previousQualification,
            QualificationDTO currentQualification)
        {
            var previousEvaluation = EvaluateFundingEligibilityRules(previousQualification);
            var currentEvaluation = EvaluateFundingEligibilityRules(currentQualification);

            var previousRulesByName = previousEvaluation.Rules
                .ToDictionary(r => r.RuleName, StringComparer.OrdinalIgnoreCase);

            var currentRulesByName = currentEvaluation.Rules
                .ToDictionary(r => r.RuleName, StringComparer.OrdinalIgnoreCase);

            var allRuleNames = previousRulesByName.Keys
                .Union(currentRulesByName.Keys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ruleComparisons = new List<FundingEligibilityRuleComparison>();

            foreach (var ruleName in allRuleNames)
            {
                var previousRule = previousRulesByName[ruleName];
                var currentRule = currentRulesByName[ruleName];

                ruleComparisons.Add(new FundingEligibilityRuleComparison
                {
                    RuleName = ruleName,
                    PreviousPassed = previousRule.Passed,
                    CurrentPassed = currentRule.Passed,
                    Fields = previousRule.Fields
                        .Union(currentRule.Fields, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                });
            }
    

            return new FundingEligibilityComparison
            {
                PreviousEvaluation = previousEvaluation,
                CurrentEvaluation = currentEvaluation,
                RuleComparisons = ruleComparisons
            };
        }

        
        

    public string DetermineFailureReason(QualificationDTO qualification) => ImportReason.NoAction;
}