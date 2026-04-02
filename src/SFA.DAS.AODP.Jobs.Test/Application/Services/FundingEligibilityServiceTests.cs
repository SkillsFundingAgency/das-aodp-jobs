using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using Xunit;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class FundingEligibilityServiceTests
    {
        private readonly Mock<ILogger<FundingEligibilityService>> _mockLogger;
        private readonly FundingEligibilityService _service;

        public FundingEligibilityServiceTests()
        {
            _mockLogger = new Mock<ILogger<FundingEligibilityService>>();
            _service = new FundingEligibilityService(_mockLogger.Object);
        }

        [Fact]
        public void EvaluateFundingEligibilityRules_Eligible_ReturnsTrue()
        {
            // Arrange
            var qualification = CreateEligibleBaseline();

            // Act
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.True(evaluation.IsEligible);
            Assert.Empty(evaluation.GetFailedFields());
        }

        [Fact]
        public void EvaluateFundingEligibilityRules_Ineligible_IntentionToSeekFundingInEngland()
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.IntentionToSeekFundingInEngland = false;

            // Act
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.False(evaluation.IsEligible);
            Assert.Contains("IntentionToSeekFundingInEngland", evaluation.GetFailedFields());
        }

        [Fact]
        public void EvaluateFundingEligibilityRules_Ineligible_OfferedInEngland()
        {
            var qualification = CreateEligibleBaseline();
            qualification.OfferedInEngland = false;

            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            Assert.False(evaluation.IsEligible);
            Assert.Contains("OfferedInEngland", evaluation.GetFailedFields());
        }

        [Fact]
        public void EvaluateFundingEligibilityRules_Ineligible_TqtZero()
        {
            var qualification = CreateEligibleBaseline();
            qualification.Tqt = 0;

            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            Assert.False(evaluation.IsEligible);
            Assert.Contains("Tqt", evaluation.GetFailedFields());
        }

        [Theory]
        [InlineData("Certificate in Education")]
        [InlineData("Professional Graduate Certificate in Education")]
        [InlineData("Postgraduate Diploma in Education")]
        [InlineData("ESOL International")]
        [InlineData("degree")]
        [InlineData("foundation degree")]
        [InlineData("Higher National Certificate")]
        [InlineData("Certificate of Higher Education")]
        [InlineData("Higher National Diploma")]
        [InlineData("Diploma of Higher Education")]
        [InlineData("Diploma in Teaching")]
        public void EvaluateFundingEligibilityRules_Ineligible_MatchingTitles(string title)
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.Title = $"Some {title} here";

            // Act
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.False(evaluation.IsEligible);
            Assert.Contains("Title", evaluation.GetFailedFields());
        }

        
        private static QualificationDTO CreateEligibleBaseline()
        {
            return new QualificationDTO
            {
                OfferedInEngland = true,
                IntentionToSeekFundingInEngland = true,
                Type = "GeneralQualification",
                Title = "Valid Qualification Title",
                Glh = 10,
                Tqt = 20
            };
        }
    }
}