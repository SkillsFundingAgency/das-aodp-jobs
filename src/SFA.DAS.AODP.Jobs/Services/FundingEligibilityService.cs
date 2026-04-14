using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class FundingEligibilityService : IFundingEligibilityService
    {
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
                    "TypeIsNotEndPointAssessment",
                    qualification.Type != QualificationReference.EndPointAssessment,
                    ["Type"]),

                new FundingEligibilityRuleResult(
                    "TitleDoesNotContainIneligibleQualifications",
                    !ContainsIneligibleQualification(qualification.Title),
                    ["Title"]),

                new FundingEligibilityRuleResult(
                    "TitleDoesNotContainIneligibleQualificationShortForms",
                    !ContainsIneligibleQualificationShortForm(qualification.Title),
                    ["Title"]),

                new FundingEligibilityRuleResult(
                    "GlhPresentAndGreaterThanZero",
                    qualification.Glh.HasValue && qualification.Glh.Value > 0,
                    ["Glh"]),

                new FundingEligibilityRuleResult(
                    "TqtPresentAndGreaterThanZero",
                    qualification.Tqt.HasValue && qualification.Tqt.Value > 0,
                    ["Tqt"] ),

                new FundingEligibilityRuleResult(
                    "GlhLessThanTqt",
                    qualification.Glh.HasValue
                        && qualification.Tqt.HasValue
                        && qualification.Glh <= qualification.Tqt,
                    ["TqtLessThanGlh"])
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

        private static bool ContainsIneligibleQualification(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            return QualificationReference.IneligibleQualifications.Any(s =>
                title.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ContainsIneligibleQualificationShortForm(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            return QualificationReference.IneligibleQualificationsShortForms.Any(s =>
                title.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        
    }
}