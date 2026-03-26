using AutoFixture;
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
        public void FundingEligibilityService_Eligible()
        {
            // Arrange
            var qualification = CreateEligibleBaseline();

            // Act
            var eligible = _service.EligibleForFunding(qualification);

            // Assert
            Assert.True(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_IntentionToSeekFundingInEngland()
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.IntentionToSeekFundingInEngland = false;

            // Act
            var eligible = _service.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_OfferedInEngland()
        {
            var qualification = CreateEligibleBaseline();
            qualification.OfferedInEngland = false;

            Assert.False(_service.EligibleForFunding(qualification));
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_EndPointAssessmentType()
        {
            var qualification = CreateEligibleBaseline();
            qualification.Type = QualificationReference.EndPointAssessment;

            Assert.False(_service.EligibleForFunding(qualification));
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_TqtZero()
        {
            var qualification = CreateEligibleBaseline();
            qualification.Tqt = 0;

            Assert.False(_service.EligibleForFunding(qualification));
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_GlhZero()
        {
            var qualification = CreateEligibleBaseline();
            qualification.Glh = 0;

            Assert.False(_service.EligibleForFunding(qualification));
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_GLH_Larger()
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.Glh = 10;
            qualification.Tqt = 1;

            // Act
            var eligible = _service.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
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
        public void FundingEligibilityService_Ineligible_MatchingTitle(string title)
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.Title = $"prefix {title} suffix";

            // Act
            var eligible = _service.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        [Theory]
        [InlineData("CertEd")]
        [InlineData("PGCE")]
        [InlineData("PGDE")]
        [InlineData("HNC")]
        [InlineData("Cert HE")]
        [InlineData("HND")]
        [InlineData("Dip HE")]
        [InlineData("further education and skills")]
        public void FundingEligibilityService_Ineligible_MatchingShortTitle(string shortTitle)
        {
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.Title = $"prefix {shortTitle} suffix";

            // Act
            var eligible = _service.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        private QualificationDTO CreateEligibleBaseline()
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