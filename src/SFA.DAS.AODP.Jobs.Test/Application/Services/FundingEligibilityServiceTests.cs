using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class FundingEligibilityServiceTests
    {
        private readonly Mock<ILogger<FundingEligibilityService>> _mockLogger;
        private FundingEligibilityService fundingEligibilityService;
        private Fixture _fixture;

        public FundingEligibilityServiceTests()
        {
            _mockLogger = new Mock<ILogger<FundingEligibilityService>>();
            _fixture = new Fixture();
            fundingEligibilityService = new FundingEligibilityService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeActionTypeMap()
        {
            // Act
            fundingEligibilityService = new FundingEligibilityService(_mockLogger.Object);

            // Assert
            Assert.NotNull(fundingEligibilityService);
            
        }

        [Fact]
        public void FundingEligibilityService_Eligible()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()             
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.True(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_minoperationaldate()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, DateTime.MinValue)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_GLH_Larger()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 1)
                .With(w => w.OperationalStartDate, DateTime.MinValue)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

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
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.Title, $"SomeText{title}EndText")
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)           
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

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
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.Title, $"SomeText{shortTitle}EndText")
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        [Fact]
        public void FundingEligibilityService_RejectReason_NoAction()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .Create();

            // Act
            var reason = fundingEligibilityService.DetermineFailureReason(qualification);

            // Assert
            Assert.Equal(ImportReason.NoAction, reason);
        }

        [Fact]
        public void FundingEligibilityService_RejectReason_NoHLG()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 0)
                .With(w => w.Tqt, 0)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .Create();

            // Act
            var reason = fundingEligibilityService.DetermineFailureReason(qualification);

            // Assert
            Assert.Equal(ImportReason.NoGLHOrTQT, reason);
        }
    }
}



