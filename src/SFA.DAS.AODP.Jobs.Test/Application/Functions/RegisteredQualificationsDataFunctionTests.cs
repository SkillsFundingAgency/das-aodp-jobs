using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Functions.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

public class RegisteredQualificationsDataFunctionTests
{
    [Fact]
    public async Task Run_ShouldInsertProcessedQualifications()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RegisteredQualificationsDataFunction>>();
        var dbContextMock = new Mock<IApplicationDbContext>();
        var apiServiceMock = new Mock<IQualificationsApiService>();
        var function = new RegisteredQualificationsDataFunction(loggerMock.Object, dbContextMock.Object, apiServiceMock.Object);

        var qualifications = new List<RegisteredQualificationsImport>();
        dbContextMock.Setup(db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegisteredQualificationsImport>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RegisteredQualificationsImport>, CancellationToken>((qualificationsList, cancellationToken) =>
            {
                qualifications.AddRange(qualificationsList);
            })
            .Returns(Task.CompletedTask);

        var qualificationQueryParameter = new RegisteredQualificationQueryParameters();

        apiServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegisteredQualificationQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>())
            )
            .ReturnsAsync(new PaginatedResult<RegisteredQualification>
            {
                Results = new List<RegisteredQualification>
                {
            new RegisteredQualification { QualificationNumber = "1234", Title = "Test Qualification" }
                }
            });

        var httpRequestMock = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();

        // Act
        var result = await function.Run(httpRequestMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Successfully processed 5000 qualifications.", okResult.Value);
        dbContextMock.Verify(
            db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegisteredQualificationsImport>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}