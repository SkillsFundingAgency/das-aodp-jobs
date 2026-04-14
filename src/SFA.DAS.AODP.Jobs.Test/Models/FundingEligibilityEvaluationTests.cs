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
                [
                    new FundingEligibilityRuleResult ("rule one", true, [] ) ,
                    new FundingEligibilityRuleResult ("rule two", true, []) 
                ]
            };

            // Act
            var eligible = evaluation.IsEligible;

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(eligible);
                Assert.Empty(evaluation.FailedRules);
                Assert.Empty(evaluation.GetFailedFields());
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
                    new FundingEligibilityRuleResult ("RuleA", false, [FieldA] ),
                    new FundingEligibilityRuleResult ("RuleB", false, [FieldB] )
                }
            };

            // Act
            var csv = evaluation.GetFailedFieldsCsv();

            // Assert
            Assert.Equal($"{FieldA}, {FieldB}", csv);
        }
    }
}
