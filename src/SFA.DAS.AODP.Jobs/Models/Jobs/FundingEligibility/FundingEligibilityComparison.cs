using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    public class FundingEligibilityComparison
    {
        public FundingEligibilityEvaluation PreviousEvaluation { get; set; } = new();
        public FundingEligibilityEvaluation CurrentEvaluation { get; set; } = new();

        public bool EligibilityChanged =>
            PreviousEvaluation.IsEligible != CurrentEvaluation.IsEligible;

        public List<FundingEligibilityRuleComparison> RuleComparisons { get; set; } = new();

        public IEnumerable<FundingEligibilityRuleComparison> ChangedRules =>
            RuleComparisons.Where(r => r.OutcomeChanged);

        public IEnumerable<string> GetContributingFields() =>
            ChangedRules
                .SelectMany(r => r.Fields)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
