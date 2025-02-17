﻿using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Infrastructure.Context;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using System.Collections.Specialized;
using Microsoft.Azure.Functions.Worker;
using AutoFixture;
using RestEase;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class OfqualImportServiceTests
    {
        private readonly Mock<ILogger<OfqualImportService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IApplicationDbContext> _dbContextMock;
        private readonly Mock<IOfqualRegisterApi> _apiClientMock;
        private readonly Mock<IOfqualRegisterService> _ofqualRegisterServiceMock;
        private readonly Mock<IQualificationsService> _qualificationsServiceMock;
        private readonly Mock<IActionTypeService> _actionTypeServiceMock;
        private readonly Mock<IFundingEligibilityService> _fundingEligibilityService;
        private readonly FunctionContext _functionContext;
        private readonly OfqualImportService _service;
        private Fixture _fixture;

        public OfqualImportServiceTests()
        {
            _loggerMock = new Mock<ILogger<OfqualImportService>>();
            _configurationMock = new Mock<IConfiguration>();
            _dbContextMock = new Mock<IApplicationDbContext>();
            _apiClientMock = new Mock<IOfqualRegisterApi>();
            _ofqualRegisterServiceMock = new Mock<IOfqualRegisterService>();
            _qualificationsServiceMock = new Mock<IQualificationsService>();
            _actionTypeServiceMock = new Mock<IActionTypeService>();
            _fundingEligibilityService = new Mock<IFundingEligibilityService>();
            _functionContext = new Mock<FunctionContext>().Object;
            _fixture = new Fixture();

            _service = new OfqualImportService(
                _loggerMock.Object,
                _configurationMock.Object,
                _dbContextMock.Object,
                _apiClientMock.Object,
                _ofqualRegisterServiceMock.Object,
                _qualificationsServiceMock.Object,
                _actionTypeServiceMock.Object,
                _fundingEligibilityService.Object
            );
        }

        [Fact]
        public async Task StageQualificationsDataAsync_Should_Clear_StagedQualifications()
        {
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });
            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);


            _dbContextMock.Setup(db => db.TruncateTable<QualificationImportStaging>()).Returns(Task.CompletedTask);

            await _service.StageQualificationsDataAsync(requestMock.Object);

            _dbContextMock.Verify(db => db.TruncateTable<QualificationImportStaging>(), Times.Once);
        }

        [Fact]
        public async Task StageQualificationsDataAsync_Should_Process_Qualifications()
        {
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var queryParams = new Dictionary<string, string> { { "param", "value" } };
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });
            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            await _service.StageQualificationsDataAsync(requestMock.Object);

            _qualificationsServiceMock.Verify(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()), Times.Once);
        }

        [Fact]
        public async Task StageQualificationsDataAsync_ShouldThrowApiException_WhenApiExceptionOccurs()
        {
            // Arrange
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };
            var apiException = new ApiException(requestMessage, responseMessage, "API error");

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() => _service.StageQualificationsDataAsync(requestMock.Object));
        }

        [Fact]
        public async Task StageQualificationsDataAsync_ShouldThrowSystemException_WhenSystemExceptionOccurs()
        {
            // Arrange
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };
            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ThrowsAsync(new SystemException("System error"));

            // Act & Assert
            await Assert.ThrowsAsync<SystemException>(() => _service.StageQualificationsDataAsync(requestMock.Object));
        }
    }

}

