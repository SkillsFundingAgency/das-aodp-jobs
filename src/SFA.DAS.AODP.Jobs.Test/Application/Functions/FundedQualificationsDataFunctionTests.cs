using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Jobs.Services.CSV;
using Microsoft.Azure.Functions.Worker;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using Xunit;
using SAF.DAS.AODP.Models.Qualification;
using AutoMapper;
using SFA.DAS.AODP.Data.Entities;
using Amazon;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions
{
    public class FundedQualificationsDataFunctionTests
    {
        private readonly Mock<ILogger<FundedQualificationsDataFunction>> _loggerMock;
        private readonly Mock<IApplicationDbContext> _applicationDbContextMock;
        private readonly Mock<ICsvReaderService> _csvReaderServiceMock;
        private readonly FunctionContext _functionContext;
        private readonly FundedQualificationsDataFunction _function;
        private readonly IMapper _mapper;

        public FundedQualificationsDataFunctionTests()
        {
            _loggerMock = new Mock<ILogger<FundedQualificationsDataFunction>>();
            _applicationDbContextMock = new Mock<IApplicationDbContext>();
            _csvReaderServiceMock = new Mock<ICsvReaderService>();
            _functionContext = new Mock<FunctionContext>().Object;

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new MapperProfile()));
            _mapper = new Mapper(configuration);
            
            _function = new FundedQualificationsDataFunction(
                _loggerMock.Object,
                _applicationDbContextMock.Object,
                _csvReaderServiceMock.Object,
                _mapper);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenCsvFileIsProcessedSuccessfully()
        {
            // Arrange
            var approvedQualifications = new List<FundedQualificationDTO>
            {
                new FundedQualificationDTO {QualificationName = "Test Qualification" ,Offers=new List<FundedQualificationOfferDTO>(){ new()  } }
            };
            _csvReaderServiceMock
                
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(It.IsAny<string>()))
                .ReturnsAsync(approvedQualifications);

            var httpRequestData = new MockHttpRequestData(_functionContext);
            Environment.SetEnvironmentVariable("FundedQualificationsImportUrl", "https://example.com/approved.csv");
            Environment.SetEnvironmentVariable("ArchivedFundedQualificationsImportUrl", "https://example.com/archived.csv");
            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(It.IsAny<IEnumerable<FundedQualification>>(), default), Times.Exactly(2));
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
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(It.IsAny<IEnumerable<FundedQualification>>(), default), Times.Never);
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
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _csvReaderServiceMock.Verify(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(It.IsAny<string>()), Times.Never);
            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(It.IsAny<IEnumerable<FundedQualificationDTO>>(), default), Times.Never);
        }
    }
}

