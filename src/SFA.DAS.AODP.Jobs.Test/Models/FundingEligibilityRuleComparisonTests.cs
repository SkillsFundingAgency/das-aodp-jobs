using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Fields = new() { FieldA }
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
