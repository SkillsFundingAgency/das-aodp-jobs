using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    public class FundingEligibilityEvaluation
    {
        public List<FundingEligibilityRuleResult> Rules { get; set; } = new();

        public bool IsEligible => Rules.All(r => r.Passed);

        public List<FundingEligibilityRuleResult> FailedRules =>
            Rules.Where(r => !r.Passed).ToList();

        public List<string> FailedFields =>
            FailedRules
                .SelectMany(r => r.Fields)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        public string FailedFieldsCsv =>
            string.Join(", ", FailedFields ?? Enumerable.Empty<string>());
    }
}
