using AutoFixture;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Infrastructure.Interfaces;
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
        private readonly Mock<IFileProcessingService> _fileProcessingService;
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
                FunctionHostKey = "???"
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
            _fileProcessingService = new Mock<IFileProcessingService>();
            _qualificationVersionRepository = new Mock<IQualificationVersionRepository>();

            _function = new FundedQualificationsDataFunction(
                _loggerMock.Object,
                _csvReaderServiceMock.Object,
                _config,
                _jobConfigurationService.Object,
                _fundedQualificationWriter.Object,
                _qualificationsRepository.Object,
                _qualificationVersionRepository.Object,
                _fileProcessingService.Object);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenBothFilesReadyAndHaveData()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();
            var fundedImport = _fixture.CreateMany<FundedQualificationDTO>(20).ToList();
            var archivedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 2 })));

            _csvReaderServiceMock
                .SetupSequence(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport)
                .ReturnsAsync(archivedImport);

            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Returns(Task.CompletedTask);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(archivedImport)).ReturnsAsync(true);
            _fundedQualificationWriter.Setup(s => s.SeedFundingData()).ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<OkObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenApprovedCsvIsEmpty()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(new List<FundedQualificationDTO>());

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<NotFoundObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldStatusCode500_WhenSystemExceptionThrown()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData, "user");

            var result = Assert.IsType<StatusCodeResult>(response);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenApprovedFileNotReady()
        {
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(false, false, null));

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData, "user");

            var ok = Assert.IsType<OkObjectResult>(response);
            Assert.Contains("File not ready", ok.Value?.ToString());
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenArchivedFileNotReady()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();
            var fundedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(false, false, null));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport);

            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Returns(Task.CompletedTask);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData, "user");

            var ok = Assert.IsType<OkObjectResult>(response);
            Assert.Contains("File not ready", ok.Value?.ToString());
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenJobDisabled()
        {
            _control.JobEnabled = false;
            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");
            Assert.IsType<OkObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenJobAlreadyRunning()
        {
            _control.Status = JobStatus.Running.ToString();
            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");
            Assert.IsType<OkObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenArchivedCsvIsEmpty()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();
            var fundedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 2 })));

            _csvReaderServiceMock
                .SetupSequence(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport)
                .ReturnsAsync(new List<FundedQualificationDTO>());

            _qualificationsRepository.Setup(s => s.TruncateFundingTables()).Returns(Task.CompletedTask);
            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<NotFoundObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnApiStatusCode_WhenApiExceptionThrown()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            var response = new HttpResponseMessage((HttpStatusCode)418)
            {
                ReasonPhrase = "Failure reason",
                Content = new StringContent("fail")
            };

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ThrowsAsync(new ApiException(request, response, "fail"));

            var httpRequestData = new MockHttpRequestData(_functionContext);
            var result = await _function.Run(httpRequestData, "user");

            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(418, status.StatusCode);
        }


        [Fact]
        public async Task Run_ShouldNotSeedFundingData_WhenNoRecordsProcessed()
        {
            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(new List<FundedQualificationDTO>());

            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<NotFoundObjectResult>(response);
            _fundedQualificationWriter.Verify(s => s.SeedFundingData(), Times.Never);
        }

        [Fact]
        public async Task Run_ShouldSkipFundedImport_WhenImportFundedCsvIsFalse()
        {
            _control.ImportFundedCsv = false;

            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();
            var archivedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 2 })));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(archivedImport);

            _fundedQualificationWriter.Setup(s => s.WriteQualifications(archivedImport)).ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<OkObjectResult>(response);
            _fundedQualificationWriter.Verify(s => s.WriteQualifications(It.IsAny<List<FundedQualificationDTO>>()), Times.Once);
        }

        [Fact]
        public async Task Run_ShouldSkipArchivedImport_WhenImportArchivedCsvIsFalse()
        {
            _control.ImportArchivedCsv = false;

            var qualificationLookups = _fixture.CreateMany<QualificationLookupItem>(20).ToList();
            var fundedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _qualificationVersionRepository
                .Setup(s => s.GetLatestQualificationVersionSnapshotsAsync())
                .ReturnsAsync(qualificationLookups);

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualificationLookups,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport);

            _fundedQualificationWriter.Setup(s => s.WriteQualifications(fundedImport)).ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);
            var response = await _function.Run(httpRequestData, "user");

            Assert.IsType<OkObjectResult>(response);
            _fundedQualificationWriter.Verify(s => s.WriteQualifications(fundedImport), Times.Once);
        }

    }
}
