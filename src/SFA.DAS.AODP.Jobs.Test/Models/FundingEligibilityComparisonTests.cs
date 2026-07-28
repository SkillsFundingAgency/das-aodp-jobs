using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.UnitTests.Models
{
    public class FundingEligibilityComparisonTests
    {
        private const string FieldA = "Glh";

        [Fact]
        public void EligibilityChanged_WhenEligibilityDiffers_ReturnsTrue()
        {
            // Arrange
            var previous = new FundingEligibilityEvaluation
            {
                Rules = { new FundingEligibilityRuleResult("Passed rule", true, []) }
            };

            var current = new FundingEligibilityEvaluation
            {
                Rules = { new FundingEligibilityRuleResult("Failed rule", false, [FieldA]) }
            };

            var comparison = new FundingEligibilityComparison
            {
                PreviousEvaluation = previous,
                CurrentEvaluation = current,
                RuleComparisons =
                [
                    new FundingEligibilityRuleComparison
                    {
                        RuleName = "GlhRule",
                        PreviousPassed= true,
                        CurrentPassed = false,
                        Fields = [FieldA]
                    }
                    ]
            };

            // Act
            var changed = comparison.EligibilityChanged;

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(changed);
                Assert.Equal(new[] { FieldA }, comparison.GetContributingFields());
            });
        }
    }
}
