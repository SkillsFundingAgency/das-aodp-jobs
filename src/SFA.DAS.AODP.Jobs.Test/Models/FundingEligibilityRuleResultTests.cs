using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.UnitTests.Models
{
    public class FundingEligibilityRuleResultTests
    {
        private const string Rule = "GlhLessThanOther";
        private const string FieldA = "Glh";
        private const string FieldB = "Tqt";

        [Fact]
        public void Constructor_AssignsValuesCorrectly()
        {
            // Arrange

            // Act
            var result = new FundingEligibilityRuleResult
            {
                RuleName = Rule,
                Passed = true,
                Fields = new() { FieldA, FieldB }
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(Rule, result.RuleName);
                Assert.True(result.Passed);
                Assert.Equal(new[] { FieldA, FieldB }, result.Fields);
            });
        }
    }
}
