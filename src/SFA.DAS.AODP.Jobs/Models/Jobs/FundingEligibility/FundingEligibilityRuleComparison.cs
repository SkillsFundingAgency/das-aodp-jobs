namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    [ExcludeFromCodeCoverage]
    public record FundingEligibilityRuleComparison
    {
        public string RuleName { get; set; } = string.Empty;
        public bool PreviousPassed { get; set; }
        public bool CurrentPassed { get; set; }
        public IReadOnlyList<string> Fields { get; init; } = new List<string>();

        public bool OutcomeChanged => PreviousPassed != CurrentPassed;
    }
}
