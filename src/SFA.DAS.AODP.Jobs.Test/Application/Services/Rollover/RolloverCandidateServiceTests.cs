using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Jobs.Services.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class RolloverCandidateServiceTests
{
    [Fact(Skip = "temp ignore")]
    public async Task GenerateRolloverCandidatesAsync_UsesCurrentAcademicYear()
    {
        // Arrange
        var repository = new Mock<IRolloverCandidateRepository>();
        var clock = new Mock<ISystemClockService>();
        var now = new DateTime(2026, 6, 28, 9, 30, 0, DateTimeKind.Utc);
        clock.Setup(x => x.UtcNow).Returns(now);
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        repository
            .Setup(x => x.CreateInitialRolloverCandidatesAsync(academicYear, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var service = new RolloverCandidateService(repository.Object, clock.Object);

        // Act
        var result = await service.GenerateRolloverCandidatesAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(7);
        repository.Verify(x => x.CreateInitialRolloverCandidatesAsync(academicYear, It.IsAny<CancellationToken>()), Times.Once);
    }
}
