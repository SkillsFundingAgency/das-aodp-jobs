namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    [ExcludeFromCodeCoverage]
    public record FundingEligibilityComparison
    {
        public FundingEligibilityEvaluation PreviousEvaluation { get; set; } = new();
        public FundingEligibilityEvaluation CurrentEvaluation { get; set; } = new();

        public bool EligibilityChanged =>
            PreviousEvaluation.IsEligible != CurrentEvaluation.IsEligible;

        public IReadOnlyList<FundingEligibilityRuleComparison> RuleComparisons { get; set; } = new List<FundingEligibilityRuleComparison>();

        public IEnumerable<FundingEligibilityRuleComparison> ChangedRules =>
            RuleComparisons.Where(r => r.OutcomeChanged);

        public IEnumerable<string> GetContributingFields() =>
            ChangedRules
                .SelectMany(r => r.Fields)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
