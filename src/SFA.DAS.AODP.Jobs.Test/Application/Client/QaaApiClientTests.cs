using SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Client;
public class QaaApiClientTests
{
    [Fact]
    public async Task GetQualificationsAsync_WhenResponseIsSuccessful_ReturnsDeserializedQualifications()
    {
        // Arrange
        var json = 
            @"[
                {
                    ""AIM_Code"": ""40000011"",
                    ""Awarding_Body"": ""LASER Learning Awards"",
                    ""Diploma_Title"": ""Access to HE Diploma (Art and Design)"",
                    ""SSA_Tier_1"": ""9"",
                    ""SSA_Tier_2"": ""2"",
                    ""Start_Date_Of_Qualification"": ""9 / 2014"",
                    ""Last_Date_For_Registrations"": ""12 / 2019"",
                    ""Last_Date_For_Certifications"": ""12 / 2024"",
                    ""Award_Status"": ""Discontinued"",
                    ""Discontinued_Date"": ""2019-03-05""
                },
                {
                    ""AIM_Code"": ""40000012"",
                    ""Awarding_Body"": ""LASER Learning Awards"",
                    ""Diploma_Title"": ""Access to HE Diploma (Construction)"",
                    ""SSA_Tier_1"": ""9"",
                    ""SSA_Tier_2"": ""2"",
                    ""Start_Date_Of_Qualification"": ""9 / 2014"",
                    ""Last_Date_For_Registrations"": ""12 / 2019"",
                    ""Last_Date_For_Certifications"": ""12 / 2024"",
                    ""Award_Status"": ""Discontinued"",
                    ""Discontinued_Date"": ""2019-03-05""
                }
            ]";

        var expected = new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = "40000011", 
                DiplomaTitle = "Access to HE Diploma (Art and Design)",
                AwardStatus = "Discontinued", 
                AwardingBody = "LASER Learning Awards", 
                SsaTier1 = "9", 
                SsaTier2 = "2",
                StartDateOfQualification = new DateOnly(2014, 9, 1),
                LastDateForCertifications = new DateOnly(2024, 12, 31),
                LastDateForRegistrations = new DateOnly(2019, 12, 31), 
                DiscontinuedDate = new DateOnly(2019, 03, 05)
            },
            new()
            {
                AimCode = "40000012", 
                DiplomaTitle = "Access to HE Diploma (Construction)",
                AwardStatus = "Discontinued", 
                AwardingBody = "LASER Learning Awards", 
                SsaTier1 = "9", 
                SsaTier2 = "2",
                StartDateOfQualification = new DateOnly(2014, 9, 1),
                LastDateForCertifications = new DateOnly(2024, 12, 31),
                LastDateForRegistrations = new DateOnly(2019, 12, 31), 
                DiscontinuedDate = new DateOnly(2019, 03, 05)
            },
        };

        var loggerMock = new Mock<ILogger<QaaApiClient>>();

        var httpClient = CreateHttpClient((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            return Task.FromResult(response);
        });

        var sut = new QaaApiClient(loggerMock.Object, httpClient);

        // Act
        var result = await sut.GetQualificationsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(expected[0].AimCode, result[0].AimCode);
        Assert.Equal(expected[0].DiplomaTitle, result[0].DiplomaTitle);
        Assert.Equal(expected[0].AwardStatus, result[0].AwardStatus);
        Assert.Equal(expected[0].AwardingBody, result[0].AwardingBody);
        Assert.Equal(expected[0].SsaTier1, result[0].SsaTier1);
        Assert.Equal(expected[0].SsaTier2, result[0].SsaTier2);
        Assert.Equal(expected[0].StartDateOfQualification, result[0].StartDateOfQualification);
        Assert.Equal(expected[0].LastDateForCertifications, result[0].LastDateForCertifications);
        Assert.Equal(expected[0].LastDateForRegistrations, result[0].LastDateForRegistrations);
        Assert.Equal(expected[0].DiscontinuedDate, result[0].DiscontinuedDate);

        Assert.Equal(expected[1].AimCode, result[1].AimCode);
        Assert.Equal(expected[1].DiplomaTitle, result[1].DiplomaTitle);
        Assert.Equal(expected[1].AwardStatus, result[1].AwardStatus);
        Assert.Equal(expected[1].AwardingBody, result[1].AwardingBody);
        Assert.Equal(expected[1].SsaTier1, result[1].SsaTier1);
        Assert.Equal(expected[1].SsaTier2, result[1].SsaTier2);
        Assert.Equal(expected[1].StartDateOfQualification, result[1].StartDateOfQualification);
        Assert.Equal(expected[1].LastDateForCertifications, result[1].LastDateForCertifications);
        Assert.Equal(expected[1].LastDateForRegistrations, result[1].LastDateForRegistrations);
        Assert.Equal(expected[1].DiscontinuedDate, result[1].DiscontinuedDate);
    }

    [Fact]
    public async Task GetQualificationsAsync_WhenHttpRequestExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QaaApiClient>>();
        var expectedException = new HttpRequestException("Boom");

        var httpClient = CreateHttpClient((_, _) => throw expectedException);

        var sut = new QaaApiClient(loggerMock.Object, httpClient);

        // Act
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.GetQualificationsAsync(CancellationToken.None));

        // Assert
        Assert.Same(expectedException, ex);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Failed to call") &&
                    state.ToString()!.Contains("diplomas/all")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQualificationsAsync_WhenResponseIsNonSuccess_LogsErrorAndRethrowsHttpRequestException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QaaApiClient>>();

        var httpClient = CreateHttpClient((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server error")
            };

            return Task.FromResult(response);
        });

        var sut = new QaaApiClient(loggerMock.Object, httpClient);

        // Act
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.GetQualificationsAsync(CancellationToken.None));

        // Assert
        Assert.NotNull(ex);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Failed to call") &&
                    state.ToString()!.Contains("diplomas/all")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        var handler = new StubHttpMessageFuncHandler(handlerFunc);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
    }
}