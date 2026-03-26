using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.UnitTests.Models
{
    public class FundingEligibilityEvaluationTests
    {
        private const string FieldA = "Glh";
        private const string FieldB = "Tqt";

        [Fact]
        public void IsEligible_WhenAllRulesPass_ReturnsTrue()
        {
            // Arrange
            var evaluation = new FundingEligibilityEvaluation
            {
                Rules =
            {
                new FundingEligibilityRuleResult { Passed = true },
                new FundingEligibilityRuleResult { Passed = true }
            }
            };

            // Act
            var eligible = evaluation.IsEligible;

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(eligible);
                Assert.Empty(evaluation.FailedRules);
                Assert.Empty(evaluation.FailedFields);
            });
        }

        [Fact]
        public void FailedFieldsCsv_WhenSomeRulesFail_ReturnsCsv()
        {
            // Arrange
            var evaluation = new FundingEligibilityEvaluation
            {
                Rules =
            {
                new FundingEligibilityRuleResult { Passed = false, Fields = new() { FieldA } },
                new FundingEligibilityRuleResult { Passed = false, Fields = new() { FieldB } }
            }
            };

            // Act
            var csv = evaluation.FailedFieldsCsv;

            // Assert
            Assert.Equal($"{FieldA}, {FieldB}", csv);
        }
    }
}
