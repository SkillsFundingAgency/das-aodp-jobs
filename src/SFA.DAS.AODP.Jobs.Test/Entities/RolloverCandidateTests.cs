using SFA.DAS.AODP.Data.Entities.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class RolloverCandidateTests : UnitTest
{
    [Fact]
    public void CreateInitialRound_WhenSourceTypeIsBlank_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            RolloverCandidate.CreateInitialRound(
                "   ",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "2025/26",
                DateTime.UtcNow,
                null));

        Assert.Equal("sourceType", exception.ParamName);
    }
}
