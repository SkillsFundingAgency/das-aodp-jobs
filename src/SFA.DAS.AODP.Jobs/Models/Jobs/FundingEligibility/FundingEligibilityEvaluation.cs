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

        public IEnumerable<FundingEligibilityRuleResult> FailedRules =>
            Rules.Where(r => !r.Passed);

        public IEnumerable<string> GetFailedFields() =>
            FailedRules
                .SelectMany(r => r.Fields)
                .Distinct(StringComparer.OrdinalIgnoreCase);

        public string GetFailedFieldsCsv() =>
            string.Join(", ", GetFailedFields());
    }
}
