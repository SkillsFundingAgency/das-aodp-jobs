using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Jobs.Test.Mocks;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class SeedQaaQualificationsDataFunctionTests
{
    private readonly Mock<IQaaQualificationSeedService> _seedServiceMock = new();
    private readonly Mock<FunctionContext> _functionContextMock = new();

    [Fact]
    public async Task Run_WhenSeedIsEnabled_CallsSeedService_AndReturnsProcessedCount()
    {
        var cancellationToken = CancellationToken.None;
        _functionContextMock.SetupGet(context => context.CancellationToken).Returns(cancellationToken);
        _seedServiceMock.Setup(service => service.SeedAsync(cancellationToken)).ReturnsAsync(12);
        var function = CreateFunction();
        var request = new MockHttpRequestData(_functionContextMock.Object);

        var result = await function.Run(request, _functionContextMock.Object);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("12 records seeded", ok.Value?.ToString() ?? string.Empty);
        _seedServiceMock.Verify(service => service.SeedAsync(cancellationToken), Times.Once);
    }

    private SeedQaaQualificationsDataFunction CreateFunction()
    {
        return new SeedQaaQualificationsDataFunction(
            NullLogger<SeedQaaQualificationsDataFunction>.Instance,
            _seedServiceMock.Object);
    }
}
