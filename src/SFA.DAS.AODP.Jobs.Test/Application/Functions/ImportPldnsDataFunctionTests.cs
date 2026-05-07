using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Models.Config;
using System.Security.Claims;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class ImportPldnsDataFunctionTests
{
    private readonly Mock<ILogger<ImportPldnsDataFunction>> _loggerMock;
    private readonly Mock<IJobConfigurationService> _jobConfigurationServiceMock;
    private readonly Mock<IImportRepository> _importRepositoryMock;
    private readonly Mock<IFileProcessingService> _fileProcessingServiceMock;

    private readonly AodpJobsConfiguration _config;
    private readonly ImportPldnsDataFunction _function;
    private readonly FunctionContext _functionContext;

    public ImportPldnsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<ImportPldnsDataFunction>>();
        _jobConfigurationServiceMock = new Mock<IJobConfigurationService>();
        _importRepositoryMock = new Mock<IImportRepository>();
        _fileProcessingServiceMock = new Mock<IFileProcessingService>();

        _config = new AodpJobsConfiguration();

        _function = new ImportPldnsDataFunction(
            _loggerMock.Object,
            _config,
            _jobConfigurationServiceMock.Object,
            _importRepositoryMock.Object,
            _fileProcessingServiceMock.Object);

        _functionContext = new Mock<FunctionContext>().Object;
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldInsertParsedRecords_AndCallDeleteDuplicates()
    {
        using var stream = CreatePldnsWorkbookStream(true, 2, 3);

        _fileProcessingServiceMock
            .Setup(s => s.GetReadyFileAsync(
                FileCategory.Pldns,
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(true, false, stream));

        _importRepositoryMock
            .Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<Pldns>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _importRepositoryMock
            .Setup(r => r.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicatePldns]", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var control = new PldnsImportControl
        {
            JobId = Guid.NewGuid(),
            ImportPldns = true,
            JobEnabled = true
        };

        var lastRun = new JobRunControl
        {
            Id = Guid.NewGuid(),
            JobId = control.JobId,
            Status = "RequestSent",
            StartTime = DateTime.UtcNow
        };

        _jobConfigurationServiceMock.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastRun);
        _jobConfigurationServiceMock.Setup(s =>
            s.UpdateJobRun(It.IsAny<string>(), control.JobId, lastRun.Id, It.IsAny<int>(), JobStatus.Completed))
            .Returns(Task.CompletedTask);

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportPldns(req, "unit.test");

        Assert.IsType<OkObjectResult>(result);

        _importRepositoryMock.Verify(
            r => r.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicatePldns]", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldReturnOk_WhenFileNotReady()
    {
        _fileProcessingServiceMock
            .Setup(s => s.GetReadyFileAsync(
                It.IsAny<FileCategory>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(false, false, null));

        var control = new PldnsImportControl { JobId = Guid.NewGuid() };
        var lastRun = new JobRunControl { Id = Guid.NewGuid(), JobId = control.JobId, StartTime = DateTime.UtcNow };

        _jobConfigurationServiceMock.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastRun);

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportPldns(req, "tester");

        Assert.IsType<OkObjectResult>(result);

        _importRepositoryMock.Verify(
            r => r.BulkInsertAsync(It.IsAny<IEnumerable<Pldns>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldReturn500_WhenExceptionThrown()
    {
        _jobConfigurationServiceMock
            .Setup(s => s.ReadPldnsImportConfiguration())
            .ThrowsAsync(new Exception("boom"));

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportPldns(req, "tester");

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    private static MemoryStream CreatePldnsWorkbookStream(bool includeTargetSheet, int headerRow, int dataRow)
    {
        var ms = new MemoryStream();

        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var sheets = wbPart.Workbook.AppendChild(new Sheets());

            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            wsPart.Worksheet = new Worksheet(sheetData);

            var header = new Row { RowIndex = (uint)headerRow };
            header.Append(CreateInlineTextCell($"A{headerRow}", "text QAN"));
            sheetData.Append(header);

            var data = new Row { RowIndex = (uint)dataRow };
            data.Append(CreateInlineTextCell($"A{dataRow}", "QAN123"));
            sheetData.Append(data);

            sheets.Append(new Sheet
            {
                Id = wbPart.GetIdOfPart(wsPart),
                SheetId = 1,
                Name = includeTargetSheet ? "PLDNS V12F" : "OTHER"
            });

            wbPart.Workbook.Save();
        }

        ms.Position = 0;
        return ms;
    }

    private static Cell CreateInlineTextCell(string cellRef, string text)
    {
        return new Cell
        {
            CellReference = cellRef,
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(text))
        };
    }

    private class MockHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _body = new MemoryStream();

        public MockHttpRequestData(FunctionContext context) : base(context) { }

        public override Stream Body => _body;
        public override HttpHeadersCollection Headers { get; } = new();
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
        public override Uri Url { get; } = new Uri("http://localhost");
        public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();
        public override string Method => "GET";

        public override HttpResponseData CreateResponse()
        {
            var response = new Mock<HttpResponseData>(FunctionContext);
            response.SetupAllProperties();
            response.Object.StatusCode = HttpStatusCode.OK;
            return response.Object;
        }
    }
}
