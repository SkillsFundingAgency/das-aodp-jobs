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
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Config;
using System.Net;
using System.Security.Claims;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class ImportDefundingListDataFunctionTests
{
    private readonly Mock<ILogger<ImportDefundingListDataFunction>> _loggerMock;
    private readonly Mock<IJobConfigurationService> _jobConfigurationServiceMock;
    private readonly Mock<IImportRepository> _importRepositoryMock;
    private readonly Mock<IBlobStorageFileService> _blobServiceMock;
    private readonly AodpJobsConfiguration _config;
    private readonly ImportDefundingListDataFunction _function;
    private readonly FunctionContext _functionContext;
    private static readonly string[] stringArray =
                    // row values: QAN, Title, InScope, Comments
                    ["QAN-001", " Title one ", "0", "comment 1"];

    public ImportDefundingListDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<ImportDefundingListDataFunction>>();
        _jobConfigurationServiceMock = new Mock<IJobConfigurationService>();
        _importRepositoryMock = new Mock<IImportRepository>();
        _blobServiceMock = new Mock<IBlobStorageFileService>();
        _config = new AodpJobsConfiguration
        {
            DefundingListImportUrl = "https://somewhere/defunding.xlsx"
        };

        _function = new ImportDefundingListDataFunction(
            _loggerMock.Object,
            _config,
            _jobConfigurationServiceMock.Object,
            _importRepositoryMock.Object,
            _blobServiceMock.Object);

        _functionContext = new Mock<FunctionContext>().Object;
    }

    [Fact]
    public async Task ImportDefundingList_ShouldParseRows_HandleVariousInScopeValues_AndCallRepositoryAndUpdateJobRun()
    {
        // Arrange - create spreadsheet with target sheet, header and multiple data rows using different in-scope representations
        using var ms = CreateDefundingWorkbookStream(includeTargetSheet: true, headerRowIndex: 2, dataRows: new[]
        { stringArray,       
                ["QAN-002", "Title two", "1", ""],                
                ["QAN-003", "Title three", "Excluded", "c3"],     
                ["QAN-004", "  ", "", "    "],              
                ["QAN-005", "Title five", "Yes", "ok"]          
            });

        ms.Position = 0;
        var downloadedStream = new MemoryStream();
        await ms.CopyToAsync(downloadedStream);
        downloadedStream.Position = 0;

        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.DefundingListImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadedStream);

        var captured = new List<DefundingList>();
        _importRepositoryMock
            .Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<DefundingList>, CancellationToken>((items, ct) =>
            {
                captured.AddRange(items);
                return Task.CompletedTask;
            });

        _importRepositoryMock
            .Setup(r => r.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicateDefundingLists]", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Verifiable();

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
        _jobConfiguration_service_Setup_UpdateJobRun(control, lastJobRun);

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportDefundingList(req, "unit.user");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("records imported", ok.Value?.ToString() ?? string.Empty);

        // Verify repository calls
        _importRepositoryMock.Verify(r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()), Times.Once);
        _importRepositoryMock.Verify(r => r.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicateDefundingLists]", null, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(5, captured.Count);

        var first = captured.Single(x => x.Qan == "QAN-001");
        Assert.False(first.InScope);
        Assert.Equal("Title one", first.Title!.Trim());

        var second = captured.Single(x => x.Qan == "QAN-002");
        Assert.True(second.InScope);

        var third = captured.Single(x => x.Qan == "QAN-003");
        Assert.False(third.InScope);

        var fourth = captured.Single(x => x.Qan == "QAN-004");
        Assert.True(fourth.InScope);
        Assert.Null(fourth.Title); // whitespace title becomes null

        var fifth = captured.Single(x => x.Qan == "QAN-005");
        Assert.True(fifth.InScope);

        _jobConfigurationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task ImportDefundingList_ShouldReturnOkAndNotInsert_WhenSheetMissing()
    {
        // Arrange - workbook present but sheet name different
        using var ms = CreateDefundingWorkbookStream(includeTargetSheet: false, headerRowIndex: 2, dataRows: Array.Empty<string[]>());
        ms.Position = 0;
        var downloadedStream = new MemoryStream();
        await ms.CopyToAsync(downloadedStream);
        downloadedStream.Position = 0;

        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.DefundingListImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadedStream);

        var control = new DefundingListImportControl
        {
            JobId = Guid.NewGuid(),
            JobRunId = Guid.NewGuid(),
            ImportDefundingList = true,
            JobEnabled = true,
            Status = "Initial"
        };

        var lastJobRun = new JobRunControl { Id = Guid.NewGuid(), JobId = control.JobId, User = "u", Status = "RequestSent" };

        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfiguration_service_Setup_UpdateJobRun(control, lastJobRun);

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportDefundingList(req, "userX");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("records imported", ok.Value?.ToString() ?? string.Empty);

        _importRepositoryMock.Verify(r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()), Times.Never);
        _importRepositoryMock.Verify(r => r.DeleteDuplicateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _jobConfigurationServiceMock.Verify(s => s.UpdateJobRun("userX", control.JobId, lastJobRun.Id, 0, It.IsAny<JobStatus>()), Times.Once);
    }

    [Fact]
    public async Task ImportDefundingList_ShouldReturnOkAndNotInsert_WhenRowsInsufficient()
    {
        // Arrange - target sheet present but only one row
        using var ms = CreateDefundingWorkbookStream(includeTargetSheet: true, headerRowIndex: 1, dataRows: Array.Empty<string[]>());
        ms.Position = 0;
        var downloadedStream = new MemoryStream();
        await ms.CopyToAsync(downloadedStream);
        downloadedStream.Position = 0;

        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.DefundingListImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadedStream);

        var control = new DefundingListImportControl
        {
            JobId = Guid.NewGuid(),
            JobRunId = Guid.NewGuid(),
            ImportDefundingList = true,
            JobEnabled = true,
            Status = "Initial"
        };

        var lastJobRun = new JobRunControl { Id = Guid.NewGuid(), JobId = control.JobId, User = "tester", Status = "RequestSent" };

        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfiguration_service_Setup_UpdateJobRun(control, lastJobRun);

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportDefundingList(req, "tester1");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("records imported", ok.Value?.ToString() ?? string.Empty);

        _importRepositoryMock.Verify(r => r.BulkInsertAsync(It.IsAny<IEnumerable<DefundingList>>(), It.IsAny<CancellationToken>()), Times.Never);
        _importRepositoryMock.Verify(r => r.DeleteDuplicateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _jobConfigurationServiceMock.Verify(s => s.UpdateJobRun("tester1", control.JobId, lastJobRun.Id, 0, It.IsAny<JobStatus>()), Times.Once);
    }

    private void _jobConfiguration_service_Setup_UpdateJobRun(DefundingListImportControl control, JobRunControl lastJobRun)
    {
        _jobConfigurationServiceMock.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfigurationServiceMock.Setup(s => s.UpdateJobRun(It.IsAny<string>(), control.JobId, lastJobRun.Id, It.IsAny<int>(), It.IsAny<JobStatus>())).Returns(Task.CompletedTask).Verifiable();
    }

    private static MemoryStream CreateDefundingWorkbookStream(bool includeTargetSheet, int headerRowIndex, string[][] dataRows)
    {
        // headerRowIndex is the row index where header will be placed (1-based)
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

            // Ensure there is at least one row (title)
            var titleRow = new Row { RowIndex = 1 };
            titleRow.Append(CreateInlineTextCell("A1", "Defunding list"));
            sheetData.Append(titleRow);

            // Header row
            var header = new Row { RowIndex = (uint)headerRowIndex };
            header.Append(CreateInlineTextCell($"A{headerRowIndex}", "Qualification number"));
            header.Append(CreateInlineTextCell($"B{headerRowIndex}", "Title"));
            header.Append(CreateInlineTextCell($"C{headerRowIndex}", "In Scope"));
            header.Append(CreateInlineTextCell($"D{headerRowIndex}", "Comments"));
            sheetData.Append(header);

            // Data rows following header index
            var rowIndex = headerRowIndex + 1;
            if (dataRows != null)
            {
                foreach (var values in dataRows)
                {
                    var dataRow = new Row { RowIndex = (uint)rowIndex };
                    // A - QAN
                    dataRow.Append(CreateInlineTextCell($"A{rowIndex}", values.ElementAtOrDefault(0) ?? string.Empty));
                    // B - Title
                    dataRow.Append(CreateInlineTextCell($"B{rowIndex}", values.ElementAtOrDefault(1) ?? string.Empty));
                    // C - InScope
                    dataRow.Append(CreateInlineTextCell($"C{rowIndex}", values.ElementAtOrDefault(2) ?? string.Empty));
                    // D - Comments
                    dataRow.Append(CreateInlineTextCell($"D{rowIndex}", values.ElementAtOrDefault(3) ?? string.Empty));
                    sheetData.Append(dataRow);
                    rowIndex++;
                }
            }

            var sheetName = includeTargetSheet ? "Approval not extended" : "NOT THE RIGHT SHEET";
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1U,
                Name = sheetName
            });

            workbookPart.Workbook.Save();
        }

        ms.Position = 0;
        var outMs = new MemoryStream();
        ms.Position = 0;
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
