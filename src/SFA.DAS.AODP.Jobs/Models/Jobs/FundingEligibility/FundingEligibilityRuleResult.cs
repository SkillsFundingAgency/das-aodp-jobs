using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    public class FundingEligibilityRuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public List<string> Fields { get; set; } = new();
    }
}
