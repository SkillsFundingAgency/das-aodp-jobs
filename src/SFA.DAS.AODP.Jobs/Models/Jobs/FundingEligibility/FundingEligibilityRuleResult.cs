namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    [ExcludeFromCodeCoverage]
    public record FundingEligibilityRuleResult
    {
        public string RuleName { get; init; } = string.Empty;
        public bool Passed { get; init; }
        public IReadOnlyList<string> Fields { get; init; } = new List<string>();

        public FundingEligibilityRuleResult(string ruleName, bool passed, IReadOnlyList<string> fields)
        {
            RuleName = ruleName;
            Passed = passed;
            Fields = fields;
        }
    }
}
