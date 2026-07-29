using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Infrastructure.Repositories.QualificationVersionRepository;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions
{
    public class FundedQualificationsDataFunctionTests
    {
        private readonly Mock<ILogger<FundedQualificationsDataFunction>> _loggerMock;
        private readonly Mock<ICsvReaderService> _csvReaderServiceMock;
        private readonly Mock<IJobConfigurationService> _jobConfigurationService;
        private readonly Mock<IFundedQualificationWriter> _fundedQualificationWriter;
        private readonly Mock<IQualificationsRepository> _qualificationsRepository;
        private readonly Mock<IQualificationVersionRepository> _qualificationVersionRepository;
        private readonly Mock<IRolloverCandidateService> _rolloverCandidateService;
        private readonly FunctionContext _functionContext;
        private readonly FundedQualificationsDataFunction _function;
        private readonly AodpJobsConfiguration _config;
        private FundedJobControl _control;
        private JobRunControl _jobRunControl;
        private Fixture _fixture;

        public FundedQualificationsDataFunctionTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                    .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));
            _loggerMock = new Mock<ILogger<FundedQualificationsDataFunction>>();           
            _csvReaderServiceMock = new Mock<ICsvReaderService>();
            _functionContext = new Mock<FunctionContext>().Object;            
            _config = new AodpJobsConfiguration()
            {
                 FunctionAppBaseUrl = "https://localhost:7001",
                 FunctionHostKey = "???",
                 FundedQualificationsImportUrl = "https://localhost:5000/Funded.csv",
                 ArchivedFundedQualificationsImportUrl = "https://localhost:5000/archived.csv"
            };
            _control = new FundedJobControl()
            {
                ImportArchivedCsv = true,
                ImportFundedCsv = true,
                JobEnabled = true,
                JobRunId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                Status = "Initial"
            };
            _jobRunControl = _fixture.Build<JobRunControl>().With(w => w.Status, JobStatus.RequestSent.ToString()).Create();

            _jobConfigurationService = new Mock<IJobConfigurationService>();
            _jobConfigurationService.Setup(s => s.ReadFundedJobConfiguration()).ReturnsAsync(_control);
            _jobConfigurationService.Setup(s => s.UpdateJobRun(_jobRunControl.User, _jobRunControl.JobId, _jobRunControl.Id, It.IsAny<int>(), It.IsAny<JobStatus>())).Verifiable();
            _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.FundedQualifications.ToString())).ReturnsAsync(_jobRunControl);
            _fundedQualificationWriter = new Mock<IFundedQualificationWriter>();
            _qualificationsRepository = new Mock<IQualificationsRepository>();
            _qualificationVersionRepository = new Mock<IQualificationVersionRepository>();
            _rolloverCandidateService = new Mock<IRolloverCandidateService>();

            _function = new FundedQualificationsDataFunction(
                _loggerMock.Object,               
                _csvReaderServiceMock.Object,
                _config,
                _jobConfigurationService.Object,
                _fundedQualificationWriter.Object,
                _qualificationsRepository.Object,
                _qualificationVersionRepository.Object,
                _rolloverCandidateService.Object);
        }

        [Fact]
        public async Task Run_ShouldReturnOk()
        {
            // Arrange
            var qualificationLookups = _fixture.Build<QualificationLookupItem>()
                .CreateMany(20)
                .ToList();

            var fundedImport = _fixture.Build<FundedQualificationDTO>()                
                .CreateMany(20)
                .ToList();

            var archivedImport = _fixture.Build<FundedQualificationDTO>()
                .CreateMany(10)
                .ToList();

            _csvReaderServiceMock                                                                                             
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(_config.FundedQualificationsImportUrl, qualificationLookups, It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport);
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(_config.ArchivedFundedQualificationsImportUrl, qualificationLookups, It.IsAny<ILogger>()))
                .ReturnsAsync(archivedImport);

            _qualificationVersionRepository.Setup(s => s.GetLatestQualificationVersionSnapshotsAsync()).ReturnsAsync(qualificationLookups).Verifiable();
            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Verifiable(Times.Once);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(archivedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.SeedFundingData()).ReturnsAsync(true).Verifiable();
            _rolloverCandidateService
                .Setup(service => service.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0)
                .Verifiable();

            var httpRequestData = new MockHttpRequestData(_functionContext);           

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response);
            _qualificationsRepository.VerifyAll();
            _fundedQualificationWriter.VerifyAll();
            _rolloverCandidateService.VerifyAll();
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenCsvFileIsNotFound()
        {
            var qualificationLookups = _fixture.Build<QualificationLookupItem>()
                .CreateMany(20)
                .ToList();

            var archivedImport = _fixture.Build<FundedQualificationDTO>()
                .CreateMany(10)
                .ToList();

            var fundedImport = _fixture.Build<FundedQualificationDTO>()
                .CreateMany(20)
                .ToList();


            _qualificationVersionRepository.Setup(s => s.GetLatestQualificationVersionSnapshotsAsync()).ReturnsAsync(qualificationLookups).Verifiable();
            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Verifiable(Times.Once);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(archivedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.SeedFundingData()).ReturnsAsync(true).Verifiable();

            // Arrange
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(_config.FundedQualificationsImportUrl, qualificationLookups, It.IsAny<ILogger>()))
                .ReturnsAsync(new List<FundedQualificationDTO>());

            var httpRequestData = new MockHttpRequestData(_functionContext);          

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<NotFoundObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldStatusCode_WhenException()
        {
            var qualificationLookups = _fixture.Build<QualificationLookupItem>()
                .CreateMany(20)
                .ToList();

            var fundedImport = _fixture.Build<FundedQualificationDTO>()
                .CreateMany(20)
                .ToList();

            var archivedImport = _fixture.Build<FundedQualificationDTO>()
                .CreateMany(10)
                .ToList();

            _qualificationVersionRepository.Setup(s => s.GetLatestQualificationVersionSnapshotsAsync()).ReturnsAsync(qualificationLookups).Verifiable();
            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Verifiable(Times.Once);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(archivedImport)).ReturnsAsync(true).Verifiable();
            _fundedQualificationWriter.Setup(s => s.SeedFundingData()).ReturnsAsync(true).Verifiable();

            // Arrange
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(_config.FundedQualificationsImportUrl, qualificationLookups, It.IsAny<ILogger>()));

            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<StatusCodeResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnBadRequest_WhenUrlIsNotSet()
        {
            // Arrange 
            _config.FundedQualificationsImportUrl = "";
            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var okResult = Assert.IsType<BadRequestObjectResult>(response);
            _csvReaderServiceMock.Verify(service => service.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(It.IsAny<string>()), Times.Never);
        }
    }
}
