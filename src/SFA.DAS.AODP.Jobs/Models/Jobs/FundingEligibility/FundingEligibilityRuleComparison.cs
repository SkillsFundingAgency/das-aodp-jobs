using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    public class FundingEligibilityRuleComparison
    {
        public string RuleName { get; set; } = string.Empty;
        public bool PreviousPassed { get; set; }
        public bool CurrentPassed { get; set; }
        public List<string> Fields { get; set; } = new();

        public bool OutcomeChanged => PreviousPassed != CurrentPassed;
    }
}
