//using Moq;
//using Moq.Protected;
//using Newtonsoft.Json;
//using RestEase;
//using SFA.DAS.AODP.Data;
//using SFA.DAS.AODP.Jobs.Client;
//using SFA.DAS.AODP.Models.Qualification;
//using System.Net;
//using System.Text;

//namespace SFA.DAS.AODP.Jobs.Test.Application.Client
//{
//    public class OfqualRegisterApiTests
//    {
//        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
//        private readonly HttpClient _httpClient;
//        private readonly IOfqualRegisterApi _api;
//        private const string BaseUrl = "https://test-api.com/";

//        public OfqualRegisterApiTests()
//        {
//            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
//            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
//            {
//                BaseAddress = new Uri(BaseUrl)
//            };
//            _api = RestClient.For<IOfqualRegisterApi>(_httpClient);
//            _api.SubscriptionKey = "test-subscription-key";
//        }

//        [Fact]
//        public async Task SearchPrivateQualificationsAsync_WithMinimalParameters_SendsCorrectRequest()
//        {
//            // Arrange
//            SetupMockResponse(new PaginatedResult<QualificationDTO>());

//            // Act
//            await _api.SearchPrivateQualificationsAsync(
//                title: "Test Qualification",
//                pageNumber: 1,
//                pageSize: 10,
//                assessmentMethods: null,
//                gradingTypes: null,
//                awardingOrganisations: null,
//                availability: null,
//                qualificationTypes: null,
//                qualificationLevels: null,
//                nationalAvailability: null,
//                sectorSubjectAreas: null,
//                minTotalQualificationTime: null,
//                maxTotalQualificationTime: null,
//                minGuidedLearninghours: null,
//                maxGuidedLearninghours: null
//            );

//            // Assert
//            VerifyRequestSent(HttpMethod.Get,
//                "gov/Qualifications?title=Test%20Qualification&page=1&limit=10");
//        }

//        [Fact]
//        public async Task SearchPrivateQualificationsAsync_WithAllParameters_SendsCorrectRequest()
//        {
//            // Arrange
//            SetupMockResponse(new PaginatedResult<QualificationDTO>());

//            // Act
//            await _api.SearchPrivateQualificationsAsync(
//                title: "Test",
//                pageNumber: 1,
//                pageSize: 10,
//                assessmentMethods: "Written,Practical",
//                gradingTypes: "Pass/Fail",
//                awardingOrganisations: "Org1,Org2",
//                availability: "Current",
//                qualificationTypes: "Type1",
//                qualificationLevels: "Level1",
//                nationalAvailability: "England",
//                sectorSubjectAreas: "IT",
//                minTotalQualificationTime: 100,
//                maxTotalQualificationTime: 200,
//                minGuidedLearninghours: 50,
//                maxGuidedLearninghours: 150
//            );

//            // Assert
//            VerifyRequestSent(HttpMethod.Get,
//                "gov/Qualifications?title=Test&page=1&limit=10" +
//                "&assessmentMethods=Written%2CPractical" +
//                "&gradingTypes=Pass%2FFail" +
//                "&awardingOrganisations=Org1%2COrg2" +
//                "&availability=Current" +
//                "&qualificationTypes=Type1" +
//                "&qualificationLevels=Level1" +
//                "&nationalAvailability=England" +
//                "&sectorSubjectAreas=IT" +
//                "&minTotalQualificationTime=100" +
//                "&maxTotalQualificationTime=200" +
//                "&minGuidedLearninghours=50" +
//                "&maxGuidedLearninghours=150");
//        }

//        [Fact]
//        public async Task SearchPrivateQualificationsAsync_WithSuccessfulResponse_DeserializesCorrectly()
//        {
//            // Arrange
//            var expectedResponse = new PaginatedResult<QualificationDTO>
//            {
//                PageNumber = 1,
//                PageSize = 10,
//                TotalItems = 1,
//                Results = new List<QualificationDTO>
//            {
//                new QualificationDTO
//                {
//                    QualificationNumber = "TEST001",
//                    Title = "Test Qualification"
//                }
//            }
//            };

//            SetupMockResponse(expectedResponse);

//            // Act
//            var result = await _api.SearchPrivateQualificationsAsync(
//                title: "Test",
//                pageNumber: 1,
//                pageSize: 10,
//                assessmentMethods: null,
//                gradingTypes: null,
//                awardingOrganisations: null,
//                availability: null,
//                qualificationTypes: null,
//                qualificationLevels: null,
//                nationalAvailability: null,
//                sectorSubjectAreas: null,
//                minTotalQualificationTime: null,
//                maxTotalQualificationTime: null,
//                minGuidedLearninghours: null,
//                maxGuidedLearninghours: null
//            );

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(expectedResponse.PageNumber, result.PageNumber);
//            Assert.Equal(expectedResponse.PageSize, result.PageSize);
//            Assert.Equal(expectedResponse.TotalItems, result.TotalItems);
//            Assert.Single(result.Results);
//            Assert.Equal(expectedResponse.Results[0].QualificationNumber, result.Results[0].QualificationNumber);
//            Assert.Equal(expectedResponse.Results[0].Title, result.Results[0].Title);
//        }

//        [Fact]
//        public async Task SearchPrivateQualificationsAsync_SetsSubscriptionKeyHeader()
//        {
//            // Arrange
//            SetupMockResponse(new PaginatedResult<QualificationDTO>());
//            const string expectedSubscriptionKey = "test-subscription-key";

//            // Act
//            await _api.SearchPrivateQualificationsAsync(
//                title: "Test",
//                pageNumber: 1,
//                pageSize: 10,
//                assessmentMethods: null,
//                gradingTypes: null,
//                awardingOrganisations: null,
//                availability: null,
//                qualificationTypes: null,
//                qualificationLevels: null,
//                nationalAvailability: null,
//                sectorSubjectAreas: null,
//                minTotalQualificationTime: null,
//                maxTotalQualificationTime: null,
//                minGuidedLearninghours: null,
//                maxGuidedLearninghours: null
//            );

//            // Assert
//            VerifyHeaderSent("Ocp-Apim-Subscription-Key", expectedSubscriptionKey);
//        }

//        private void SetupMockResponse(PaginatedResult<QualificationDTO> response)
//        {
//            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
//            {
//                Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
//            };

//            _mockHttpMessageHandler
//                .Protected()
//                .Setup<Task<HttpResponseMessage>>(
//                    "SendAsync",
//                    ItExpr.IsAny<HttpRequestMessage>(),
//                    ItExpr.IsAny<CancellationToken>())
//                .ReturnsAsync(mockResponse);
//        }

//        private void VerifyRequestSent(HttpMethod method, string expectedRelativeUrl)
//        {
//            _mockHttpMessageHandler
//                .Protected()
//                .Verify(
//                    "SendAsync",
//                    Times.Once(),
//                    ItExpr.Is<HttpRequestMessage>(req =>
//                        req.Method == method &&
//                        req.RequestUri.PathAndQuery.EndsWith(expectedRelativeUrl)),
//                    ItExpr.IsAny<CancellationToken>());
//        }

//        private void VerifyHeaderSent(string headerName, string expectedValue)
//        {
//            _mockHttpMessageHandler
//                .Protected()
//                .Verify(
//                    "SendAsync",
//                    Times.Once(),
//                    ItExpr.Is<HttpRequestMessage>(req =>
//                        req.Headers.Contains(headerName) &&
//                        req.Headers.GetValues(headerName).First() == expectedValue),
//                    ItExpr.IsAny<CancellationToken>());
//        }
//    }
//}
