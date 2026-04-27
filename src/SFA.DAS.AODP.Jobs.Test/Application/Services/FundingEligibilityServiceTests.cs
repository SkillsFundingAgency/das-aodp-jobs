using AutoFixture;
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
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.True(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Eligible_OperationalStartDateIgnored()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.IntentionToSeekFundingInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, DateTime.MinValue)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.True(eligible);
        }

        [Fact]
        public void FundingEligibilityService_Ineligible_IntentionToSeekFundingInEngland()
        {
            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.IntentionToSeekFundingInEngland, false)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, DateTime.MinValue)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.False(eligible);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(0, 0)]
        [InlineData(0, null)]
        [InlineData(null, 0)]
        [InlineData(null, 10)]
        [InlineData(10, null)]
        [InlineData(10, 10)]
        [InlineData(10, 20)]
        [InlineData(20, 10)]
        public void FundingEligibilityService_GlhTqt_AnyValueEligible(int? glh, int? tqt)
        {
            // Note: There used to be a criteria whereby the GLH and TQT values had to be greater than 0, and GLH had to be less than the TQT,
            // but this was removed as part of the changes to the funding eligibility rules. This test ensures that any value for GLH and TQT (including 0) is now considered eligible, as long as the other criteria are met.

            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, glh)
                .With(w => w.Tqt, tqt)
                .With(w => w.OperationalStartDate, DateTime.MinValue)
                .Create();

            // Act
            var eligible = fundingEligibilityService.EligibleForFunding(qualification);

            // Assert
            Assert.True(eligible);
        }
    }
}