using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Models.Jobs;
using SFA.DAS.AODP.Models.Config;
using System.Net;
using System.Security.Claims;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class ImportDefundingListDataFunctionTests
{
    private readonly Mock<ILogger<ImportDefundingListDataFunction>> _loggerMock;
    private readonly Mock<IJobConfigurationService> _jobConfigurationServiceMock;
    private readonly Mock<IImportRepository> _importRepositoryMock;
    private readonly Mock<IFileProcessingService> _fileProcessingService;
    private readonly AodpJobsConfiguration _config;
    private readonly ImportDefundingListDataFunction _function;
    private readonly FunctionContext _functionContext;

    private static readonly string[] stringArray =
        ["QAN-001", " Title one ", "0", "comment 1"];

    public ImportDefundingListDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<ImportDefundingListDataFunction>>();
        _jobConfigurationServiceMock = new Mock<IJobConfigurationService>();
        _importRepositoryMock = new Mock<IImportRepository>();
        _fileProcessingService = new Mock<IFileProcessingService>();
        _config = new AodpJobsConfiguration();

        _function = new ImportDefundingListDataFunction(
            _loggerMock.Object,
            _config,
            _jobConfigurationServiceMock.Object,
            _importRepositoryMock.Object,
            _fileProcessingService.Object);

        _functionContext = new Mock<FunctionContext>().Object;
    }

    [Fact]
    public async Task ImportDefundingList_ShouldReturnOkAndNotInsert_WhenRowsInsufficient()
    {
        using var stream = CreateMinimalDefundingListXlsx();

        _fileProcessingService
            .Setup(s => s.GetReadyFileAsync(
                It.IsAny<FileCategory>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(true, false, stream));

        var control = new DefundingListImportControl
        {
            JobId = Guid.NewGuid(),
            JobRunId = Guid.NewGuid(),
            ImportDefundingList = true,
            JobEnabled = true,
            Status = "Initial"
        };

        var lastJobRun = new JobRunControl
        {
            Id = Guid.NewGuid(),
            JobId = control.JobId,
            User = "tester",
            Status = "RequestSent",
            StartTime = DateTime.UtcNow
        };

        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfigurationServiceMock.Setup(s =>
            s.UpdateJobRun("tester1", control.JobId, lastJobRun.Id, 0, It.IsAny<JobStatus>()))
            .Returns(Task.CompletedTask);

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportDefundingList(req, "tester1");

        Assert.IsType<OkObjectResult>(result);

        _importRepositoryMock.Verify(
            r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportDefundingList_ShouldReturnOk_WhenFileNotReady()
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

        var control = new DefundingListImportControl
        {
            JobId = Guid.NewGuid(),
            JobEnabled = true
        };

        var lastJobRun = new JobRunControl
        {
            Id = Guid.NewGuid(),
            JobId = control.JobId,
            Status = "RequestSent",
            StartTime = DateTime.UtcNow
        };

        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportDefundingList(req, "tester1");

        Assert.IsType<OkObjectResult>(result);

        _importRepositoryMock.Verify(
            r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportDefundingList_ShouldInsert_WhenValidRowsPresent()
    {
        using var stream = CreateDefundingWorkbookStream(
            includeTargetSheet: true,
            headerRowIndex: 1,
            dataRows: new[]
            {
            new[] { "QAN-001", "Title one", "1", "comment 1" }
            });

        _fileProcessingService
            .Setup(s => s.GetReadyFileAsync(
                It.IsAny<FileCategory>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(true, false, stream));

        var control = new DefundingListImportControl
        {
            JobId = Guid.NewGuid(),
            ImportDefundingList = true,
            JobEnabled = true
        };

        var lastJobRun = new JobRunControl
        {
            Id = Guid.NewGuid(),
            JobId = control.JobId,
            Status = "RequestSent",
            StartTime = DateTime.UtcNow
        };

        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock.Setup(s =>
            s.UpdateJobRun(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<JobStatus>()))
            .Returns(Task.CompletedTask);

        var req = new MockHttpRequestData(_functionContext);

        var result = await _function.ImportDefundingList(req, "tester1");

        Assert.IsType<OkObjectResult>(result);

        _importRepositoryMock.Verify(
            r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _importRepositoryMock.Verify(
            r => r.DeleteDuplicateAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static MemoryStream CreateDefundingWorkbookStream(
        bool includeTargetSheet,
        int headerRowIndex,
        string[][] dataRows)
    {
        var ms = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var workbook = workbookPart.Workbook;
            var sheets = workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var titleRow = new Row { RowIndex = 1 };
            titleRow.Append(CreateInlineTextCell("A1", "Defunding list"));
            sheetData.Append(titleRow);

            var header = new Row { RowIndex = (uint)headerRowIndex };
            header.Append(CreateInlineTextCell($"A{headerRowIndex}", "Qualification number"));
            header.Append(CreateInlineTextCell($"B{headerRowIndex}", "Title"));
            header.Append(CreateInlineTextCell($"C{headerRowIndex}", "In Scope"));
            header.Append(CreateInlineTextCell($"D{headerRowIndex}", "Comments"));
            sheetData.Append(header);

            var rowIndex = headerRowIndex + 1;
            if (dataRows != null)
            {
                foreach (var values in dataRows)
                {
                    var dataRow = new Row { RowIndex = (uint)rowIndex };
                    dataRow.Append(CreateInlineTextCell($"A{rowIndex}", values.ElementAtOrDefault(0) ?? string.Empty));
                    dataRow.Append(CreateInlineTextCell($"B{rowIndex}", values.ElementAtOrDefault(1) ?? string.Empty));
                    dataRow.Append(CreateInlineTextCell($"C{rowIndex}", values.ElementAtOrDefault(2) ?? string.Empty));
                    dataRow.Append(CreateInlineTextCell($"D{rowIndex}", values.ElementAtOrDefault(3) ?? string.Empty));
                    sheetData.Append(dataRow);
                    rowIndex++;
                }
            }

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1U,
                Name = includeTargetSheet ? "Approval not extended" : "NOT THE RIGHT SHEET"
            });

            workbookPart.Workbook.Save();
        }

        ms.Position = 0;
        var outMs = new MemoryStream();
        ms.CopyTo(outMs);
        outMs.Position = 0;
        return outMs;
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

    private static MemoryStream CreateMinimalDefundingListXlsx()
    {
        var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(
            stream,
            SpreadsheetDocumentType.Workbook,
            true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Approval not extended"
            });

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private class MockHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _body = new MemoryStream();

        public MockHttpRequestData(FunctionContext functionContext) : base(functionContext)
        {
        }

        public override Stream Body => _body;
        public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
        public override Uri Url { get; } = new Uri("http://localhost");
        public override IEnumerable<ClaimsIdentity> Identities { get; } = Enumerable.Empty<ClaimsIdentity>();
        public override string Method { get; } = "GET";

        public override HttpResponseData CreateResponse()
        {
            var contextMock = new Mock<HttpResponseData>(MockBehavior.Loose, this.FunctionContext);
            contextMock.SetupAllProperties();
            contextMock.Object.StatusCode = HttpStatusCode.OK;
            return contextMock.Object;
        }
    }
}