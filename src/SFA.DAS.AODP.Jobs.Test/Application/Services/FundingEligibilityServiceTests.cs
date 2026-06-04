using AutoFixture;
using SFA.DAS.AODP.Models.Qualification;
using Xunit;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class FundingEligibilityServiceTests
    {
        private readonly FundingEligibilityService _service;
        private readonly Fixture _fixture;

        public FundingEligibilityServiceTests()
        {
            _fixture = new Fixture();

            _service = new FundingEligibilityService();
        }

        [Fact]
        public void Constructor_ShouldInitializeService()
        {
            // Act
            var service = new FundingEligibilityService();

            // Assert
            Assert.NotNull(service);
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
            // Arrange
            var qualification = CreateEligibleBaseline();
            qualification.OfferedInEngland = false;

            // Act
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.False(evaluation.IsEligible);
            Assert.Contains("OfferedInEngland", evaluation.GetFailedFields());
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
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.True(evaluation.IsEligible);
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
            // Note:
            // GLH/TQT validation rules were removed from FundingEligibilityService.
            // Any values are now considered valid provided the other rules pass.

            // Arrange
            var qualification = _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.IntentionToSeekFundingInEngland, true)
                .With(w => w.Glh, glh)
                .With(w => w.Tqt, tqt)
                .Create();

            // Act
            var evaluation = _service.EvaluateFundingEligibilityRules(qualification);

            // Assert
            Assert.True(evaluation.IsEligible);
        }

        private QualificationDTO CreateEligibleBaseline()
        {
            return _fixture.Build<QualificationDTO>()
                .With(w => w.OfferedInEngland, true)
                .With(w => w.IntentionToSeekFundingInEngland, true)
                .With(w => w.Type, "GeneralQualification")
                .With(w => w.Title, "Valid Qualification Title")
                .With(w => w.Level, "3")
                .With(w => w.Glh, 10)
                .With(w => w.Tqt, 20)
                .Create();
        }
    }
}