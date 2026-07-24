using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;

namespace SFA.DAS.AODP.Jobs.UnitTests.Models
{
    public class FundingEligibilityRuleComparisonTests
    {
        private const string Rule = "TitleRule";
        private const string FieldA = "Title";

        [Fact]
        public void AssigningProperties_WorksCorrectly()
        {
            // Arrange

            // Act
            var comparison = new FundingEligibilityRuleComparison
            {
                RuleName = Rule,
                PreviousPassed = false,
                CurrentPassed = true,
                Fields = [FieldA]
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(Rule, comparison.RuleName);
                Assert.False(comparison.PreviousPassed);
                Assert.True(comparison.CurrentPassed);
                Assert.Equal(new[] { FieldA }, comparison.Fields);
            });
        }
    }
}
