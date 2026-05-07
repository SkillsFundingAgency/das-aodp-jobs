using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
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
        private readonly Mock<IFundedQualificationWriter> _fundedQualificationWriter;
        private readonly Mock<IQualificationsRepository> _qualificationsRepository;
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

            _function = new FundedQualificationsDataFunction(
                _loggerMock.Object,
                _csvReaderServiceMock.Object,
                _config,
                _jobConfigurationService.Object,
                _fundedQualificationWriter.Object,
                _qualificationsRepository.Object,
                _fileProcessingService.Object);
        }

        [Fact]
        public async Task Run_ShouldReturnOk()
        {
            // Arrange
            var organisations = _fixture.CreateMany<AwardingOrganisation>(5).ToList();
            var qualifications = _fixture.CreateMany<Qualification>(20).ToList();

            var fundedImport = _fixture.CreateMany<FundedQualificationDTO>(20).ToList();
            var archivedImport = _fixture.CreateMany<FundedQualificationDTO>(10).ToList();

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1, 2, 3 })));

            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 4, 5, 6 })));


            _csvReaderServiceMock
                .SetupSequence(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    qualifications,
                    organisations,
                    It.IsAny<ILogger>()))
                .ReturnsAsync(fundedImport)
                .ReturnsAsync(archivedImport);

            _qualificationsRepository
                .Setup(s => s.GetAwardingOrganisationsAsync())
                .ReturnsAsync(organisations);

            _qualificationsRepository
                .Setup(s => s.GetQualificationsAsync())
                .ReturnsAsync(qualifications);

            _qualificationsRepository
                .Setup(s => s.TruncateFundingTables())
                .Verifiable();

            _fundedQualificationWriter
                .Setup(s => s.WriteQualifications(It.IsAny<List<FundedQualificationDTO>>()))
                .ReturnsAsync(true);

            _fundedQualificationWriter
                .Setup(s => s.SeedFundingData())
                .ReturnsAsync(true);

            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            Assert.IsType<OkObjectResult>(response);

            _qualificationsRepository.Verify();
            _fundedQualificationWriter.Verify();
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenCsvIsEmpty()
        {
            var organisations = _fixture.CreateMany<AwardingOrganisation>(5).ToList();
            var qualifications = _fixture.CreateMany<Qualification>(20).ToList();

            _qualificationsRepository
                .Setup(s => s.GetAwardingOrganisationsAsync())
                .ReturnsAsync(organisations);

            _qualificationsRepository
                .Setup(s => s.GetQualificationsAsync())
                .ReturnsAsync(qualifications);

            // Files ready
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    It.IsAny<FileCategory>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            // CSV returns empty list
            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    It.IsAny<IEnumerable<Qualification>>(),
                    It.IsAny<IEnumerable<AwardingOrganisation>>(),
                    It.IsAny<ILogger>()))
                .ReturnsAsync(new List<FundedQualificationDTO>());

            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            Assert.IsType<NotFoundObjectResult>(response);
        }


        [Fact]
        public async Task Run_ShouldStatusCode_WhenException()
        {
            // Arrange
            var organisations = _fixture.CreateMany<AwardingOrganisation>(5).ToList();
            var qualifications = _fixture.CreateMany<Qualification>(20).ToList();

            _qualificationsRepository
                .Setup(s => s.GetAwardingOrganisationsAsync())
                .ReturnsAsync(organisations);

            _qualificationsRepository
                .Setup(s => s.GetQualificationsAsync())
                .ReturnsAsync(qualifications);

            // Mock BOTH files as ready
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    It.IsAny<FileCategory>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            // Force exception during processing
            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    It.IsAny<IEnumerable<Qualification>>(),
                    It.IsAny<IEnumerable<AwardingOrganisation>>(),
                    It.IsAny<ILogger>()))
                .ThrowsAsync(new InvalidOperationException("exception occurred"));

            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            var result = Assert.IsType<StatusCodeResult>(response);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenApprovedFileNotReady()
        {
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    It.IsAny<FileCategory>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(false, false, null));

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData);

            Assert.IsType<OkObjectResult>(response);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenArchivedFileNotReady()
        {
            // Approved is ready
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ApprovedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(true, false, new MemoryStream(new byte[] { 1 })));

            // Archived is NOT ready
            _fileProcessingService
                .Setup(s => s.GetReadyFileAsync(
                    FileCategory.ArchivedFunding,
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileProcessingResult(false, false, null));

            // ✅ FIX: prevent null exception
            _csvReaderServiceMock
                .Setup(s => s.ReadCsvFromStreamAsync<
                    FundedQualificationDTO,
                    FundedQualificationsImportClassMap>(
                    It.IsAny<Stream>(),
                    It.IsAny<IEnumerable<Qualification>>(),
                    It.IsAny<IEnumerable<AwardingOrganisation>>(),
                    It.IsAny<ILogger>()))
                .ReturnsAsync(new List<FundedQualificationDTO> { new() });

            var httpRequestData = new MockHttpRequestData(_functionContext);

            var response = await _function.Run(httpRequestData);

            var ok = Assert.IsType<OkObjectResult>(response);

            Assert.Contains("not ready", ok.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}