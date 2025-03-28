﻿using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions
{
    public class FundedQualificationsDataFunctionTests
    {
        private readonly Mock<ILogger<FundedQualificationsDataFunction>> _loggerMock;
        private readonly Mock<IApplicationDbContext> _applicationDbContextMock;
        private readonly Mock<ICsvReaderService> _csvReaderServiceMock;
        private readonly Mock<IJobConfigurationService> _jobConfigurationService;
        private readonly FunctionContext _functionContext;
        private readonly FundedQualificationsDataFunction _function;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;
        private readonly AodpJobsConfiguration _config;

        public FundedQualificationsDataFunctionTests()
        {
            _loggerMock = new Mock<ILogger<FundedQualificationsDataFunction>>();
            _applicationDbContextMock = new Mock<IApplicationDbContext>();
            _csvReaderServiceMock = new Mock<ICsvReaderService>();
            _functionContext = new Mock<FunctionContext>().Object;            
            _config = new AodpJobsConfiguration();
            _jobConfigurationService = new Mock<IJobConfigurationService>();

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new MapperProfile()));
            _mapper = new Mapper(configuration);

            _function = new FundedQualificationsDataFunction(
                _loggerMock.Object,
                _applicationDbContextMock.Object,
                _csvReaderServiceMock.Object,
                _mapper,       
                _config,
                _jobConfigurationService.Object);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenCsvFileIsNotFound()
        {
            // Arrange
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(It.IsAny<string>()))
                .ReturnsAsync(new List<FundedQualificationDTO>());

            var httpRequestData = new MockHttpRequestData(_functionContext);
            Environment.SetEnvironmentVariable("FundedQualificationsImportUrl", "https://example.com/approved.csv");

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FundedQualificationsImportUrl", null);
            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<BadRequestObjectResult>(response);
            _csvReaderServiceMock.Verify(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(It.IsAny<string>()), Times.Never);
        }
    }
}