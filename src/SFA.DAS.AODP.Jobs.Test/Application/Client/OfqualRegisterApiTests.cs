using Xunit;
using Moq;
using RestEase;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using Moq.Protected;

namespace SFA.DAS.AODP.Jobs.Test.Application.Client;

public class OfqualRegisterApiTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IOfqualRegisterApi _api;
    private const string BaseUrl = "https://test-api.com/";

    public OfqualRegisterApiTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _api = RestClient.For<IOfqualRegisterApi>(_httpClient);
        _api.SubscriptionKey = "test-subscription-key";
    }

    //[Fact]
    //public async Task SearchPrivateQualificationsAsync_WithMinimalParameters_SendsCorrectRequest()
    //{
    //    // Arrange
    //    var emptyResponse = new PaginatedResult<QualificationDTO>
    //    {
    //        Results = new List<QualificationDTO>()
    //    };

    //    _mockHttpMessageHandler
    //        .Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.Is<HttpRequestMessage>(req =>
    //                req.Method == HttpMethod.Get &&
    //                req.RequestUri.ToString().Contains("gov/Qualifications") &&
    //                req.RequestUri.Query.Contains("title=Test%20Qualification") &&
    //                req.RequestUri.Query.Contains("page=1") &&
    //                req.RequestUri.Query.Contains("limit=10")),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(JsonConvert.SerializeObject(emptyResponse))
    //        });

    //    // Act
    //    await _api.SearchPrivateQualificationsAsync(
    //        title: "Test Qualification",
    //        pageNumber: 1,
    //        pageSize: 10,
    //        assessmentMethods: null,
    //        gradingTypes: null,
    //        awardingOrganisations: null,
    //        availability: null,
    //        qualificationTypes: null,
    //        qualificationLevels: null,
    //        nationalAvailability: null,
    //        sectorSubjectAreas: null,
    //        minTotalQualificationTime: null,
    //        maxTotalQualificationTime: null,
    //        minGuidedLearninghours: null,
    //        maxGuidedLearninghours: null
    //    );

    //    // Assert
    //    _mockHttpMessageHandler.Protected().Verify(
    //        "SendAsync",
    //        Times.Once(),
    //        ItExpr.Is<HttpRequestMessage>(req =>
    //            req.Method == HttpMethod.Get &&
    //            req.RequestUri.ToString().Contains("gov/Qualifications") &&
    //            req.RequestUri.Query.Contains("title=Test%20Qualification") &&
    //            req.RequestUri.Query.Contains("page=1") &&
    //            req.RequestUri.Query.Contains("limit=10")),
    //        ItExpr.IsAny<CancellationToken>()
    //    );
    //}

    [Fact]
    public async Task SearchPrivateQualificationsAsync_WithAllParameters_SendsCorrectRequest()
    {
        // Arrange
        var emptyResponse = new PaginatedResult<QualificationDTO>
        {
            Results = new List<QualificationDTO>()
        };
        SetupMockResponse(emptyResponse);

        // Act
        await _api.SearchPrivateQualificationsAsync(
            title: "Test",
            pageNumber: 1,
            pageSize: 10,
            assessmentMethods: "Written,Practical",
            gradingTypes: "Pass/Fail",
            awardingOrganisations: "Org1,Org2",
            availability: "Current",
            qualificationTypes: "Type1",
            qualificationLevels: "Level1",
            nationalAvailability: "England",
            sectorSubjectAreas: "IT",
            minTotalQualificationTime: 100,
            maxTotalQualificationTime: 200,
            minGuidedLearninghours: 50,
            maxGuidedLearninghours: 150
        );

        // Assert
        VerifyRequestSent(HttpMethod.Get,
            "gov/Qualifications?title=Test&page=1&limit=10" +
            "&assessmentMethods=Written%2CPractical" +
            "&gradingTypes=Pass%2FFail" +
            "&awardingOrganisations=Org1%2COrg2" +
            "&availability=Current" +
            "&qualificationTypes=Type1" +
            "&qualificationLevels=Level1" +
            "&nationalAvailability=England" +
            "&sectorSubjectAreas=IT" +
            "&minTotalQualificationTime=100" +
            "&maxTotalQualificationTime=200" +
            "&minGuidedLearninghours=50" +
            "&maxGuidedLearninghours=150");
    }

    [Fact]
    public async Task SearchPrivateQualificationsAsync_WithSuccessfulResponse_DeserializesCorrectly()
    {
        // Arrange
        var expectedResponse = new PaginatedResult<QualificationDTO>
        {
            CurrentPage = 1,
            Limit = 10,
            Results = new List<QualificationDTO>
            {
                new QualificationDTO
                {
                    QualificationNumber = "TEST001",
                    Title = "Test Qualification"
                }
            }
        };

        SetupMockResponse(expectedResponse);

        // Act
        var result = await _api.SearchPrivateQualificationsAsync(
            title: "Test",
            pageNumber: 1,
            pageSize: 10,
            assessmentMethods: null,
            gradingTypes: null,
            awardingOrganisations: null,
            availability: null,
            qualificationTypes: null,
            qualificationLevels: null,
            nationalAvailability: null,
            sectorSubjectAreas: null,
            minTotalQualificationTime: null,
            maxTotalQualificationTime: null,
            minGuidedLearninghours: null,
            maxGuidedLearninghours: null
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.CurrentPage, result.CurrentPage);
        Assert.Equal(expectedResponse.Limit, result.Limit);
        Assert.Single(result.Results);
        Assert.Equal(expectedResponse.Results[0].QualificationNumber, result.Results[0].QualificationNumber);
        Assert.Equal(expectedResponse.Results[0].Title, result.Results[0].Title);
    }

    [Fact]
    public async Task SearchPrivateQualificationsAsync_SetsSubscriptionKeyHeader()
    {
        // Arrange
        var emptyResponse = new PaginatedResult<QualificationDTO>
        {
            Results = new List<QualificationDTO>()
        };
        SetupMockResponse(emptyResponse);
        const string expectedSubscriptionKey = "test-subscription-key";

        // Act
        await _api.SearchPrivateQualificationsAsync(
            title: "Test",
            pageNumber: 1,
            pageSize: 10,
            assessmentMethods: null,
            gradingTypes: null,
            awardingOrganisations: null,
            availability: null,
            qualificationTypes: null,
            qualificationLevels: null,
            nationalAvailability: null,
            sectorSubjectAreas: null,
            minTotalQualificationTime: null,
            maxTotalQualificationTime: null,
            minGuidedLearninghours: null,
            maxGuidedLearninghours: null
        );

        // Assert
        VerifyHeaderSent("Ocp-Apim-Subscription-Key", expectedSubscriptionKey);
    }

    private void SetupMockResponse(PaginatedResult<QualificationDTO> response)
    {
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);
    }

    private void VerifyRequestSent(HttpMethod method, string expectedRelativeUrl)
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri.PathAndQuery.EndsWith(expectedRelativeUrl)),
                ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyHeaderSent(string headerName, string expectedValue)
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Contains(headerName) &&
                    req.Headers.GetValues(headerName).First() == expectedValue),
                ItExpr.IsAny<CancellationToken>());
    }
}
