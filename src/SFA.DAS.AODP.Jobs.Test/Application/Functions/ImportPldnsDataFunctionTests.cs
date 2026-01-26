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
using System.Globalization;
using System.Net;
using System.Security.Claims;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class ImportPldnsDataFunctionTests
{
    private readonly Mock<ILogger<ImportPldnsDataFunction>> _loggerMock;
    private readonly Mock<IJobConfigurationService> _jobConfigurationServiceMock;
    private readonly Mock<IImportRepository> _importRepositoryMock;
    private readonly Mock<IBlobStorageFileService> _blobServiceMock;
    private readonly AodpJobsConfiguration _config;
    private readonly ImportPldnsDataFunction _function;
    private readonly FunctionContext _functionContext;

    public ImportPldnsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<ImportPldnsDataFunction>>();
        _jobConfigurationServiceMock = new Mock<IJobConfigurationService>();
        _importRepositoryMock = new Mock<IImportRepository>();
        _blobServiceMock = new Mock<IBlobStorageFileService>();
        _config = new AodpJobsConfiguration
        {
            PldnsImportUrl = "https://somewhere/pldns.xlsx"
        };

        _function = new ImportPldnsDataFunction(
            _loggerMock.Object,
            _config,
            _jobConfigurationServiceMock.Object,
            _importRepositoryMock.Object,
            _blobServiceMock.Object);

        _functionContext = new Mock<FunctionContext>().Object;
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldInsertParsedRecords_AndCallDeleteDuplicates_AndUpdateJobRun()
    {
        // Arrange - create spreadsheet stream with header row and one data row
        using var ms = CreatePldnsWorkbookStream(includeTargetSheet: true, addHeaderRowIndex: 2, addDataRowIndex: 3);
        ms.Position = 0;

        var downloadedStream = new MemoryStream();
        ms.Position = 0;
        await ms.CopyToAsync(downloadedStream);
        downloadedStream.Position = 0;

        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.PldnsImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadedStream);

        var capturedInserted = new List<Pldns>();
        _importRepositoryMock
            .Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<Pldns>>(), It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<Pldns>, CancellationToken>((items, ct) =>
            {
                capturedInserted.AddRange(items);
                return Task.CompletedTask;
            });

        _importRepositoryMock
            .Setup(r => r.DeleteDuplicateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        var control = new PldnsImportControl
        {
            JobId = Guid.NewGuid(),
            JobRunId = Guid.NewGuid(),
            ImportPldns = true,
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

        _jobConfigurationServiceMock.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfigurationServiceMock.Setup(s => s.UpdateJobRun(It.IsAny<string>(), control.JobId, lastJobRun.Id, It.IsAny<int>(), It.IsAny<JobStatus>())).Returns(Task.CompletedTask).Verifiable();

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportPldns(req, "unit.test");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("records imported", ok.Value?.ToString() ?? string.Empty);

        Assert.NotEmpty(capturedInserted);
        var item = capturedInserted.First();
        Assert.Equal("QAN123", item.Qan);

        Assert.Equal(new DateTime(2023, 2, 1), item.ListUpdatedDate?.Date);

        _importRepositoryMock.Verify(r => r.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicatePldns]", null, It.IsAny<CancellationToken>()), Times.Once);
        _jobConfigurationServiceMock.Verify(s => s.UpdateJobRun("unit.test", control.JobId, lastJobRun.Id, It.IsAny<int>(), It.IsAny<JobStatus>()), Times.Once);
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldReturnOk_WhenSheetIsMissing_AndNotCallInsertOrDelete()
    {
        // Arrange - workbook present but different sheet name
        using var ms = CreatePldnsWorkbookStream(includeTargetSheet: false, addHeaderRowIndex: 2, addDataRowIndex: 3);
        ms.Position = 0;

        var downloadedStream = new MemoryStream();
        ms.Position = 0;
        await ms.CopyToAsync(downloadedStream);
        downloadedStream.Position = 0;

        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.PldnsImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadedStream);

        var control = new PldnsImportControl
        {
            JobId = Guid.NewGuid(),
            JobRunId = Guid.NewGuid(),
            ImportPldns = true,
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

        _jobConfigurationServiceMock.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(control);
        _jobConfigurationServiceMock.Setup(s => s.GetLastJobRunAsync(It.IsAny<string>())).ReturnsAsync(lastJobRun);
        _jobConfigurationServiceMock.Setup(s => s.UpdateJobRun(It.IsAny<string>(), control.JobId, lastJobRun.Id, It.IsAny<int>(), It.IsAny<JobStatus>())).Returns(Task.CompletedTask).Verifiable();

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportPldns(req, "unit.test");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("records imported", ok.Value?.ToString() ?? string.Empty);

        _importRepositoryMock.Verify(r => r.BulkInsertAsync(It.IsAny<IEnumerable<Pldns>>(), It.IsAny<CancellationToken>()), Times.Never);
        _importRepositoryMock.Verify(r => r.DeleteDuplicateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _jobConfigurationServiceMock.Verify(s => s.UpdateJobRun("unit.test", control.JobId, lastJobRun.Id, 0, It.IsAny<JobStatus>()), Times.Once);
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldReturn500_ForSystemException()
    {
        // Arrange - job config throws SystemException
        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.PldnsImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        _jobConfigurationServiceMock
            .Setup(s => s.ReadPldnsImportConfiguration())
            .ThrowsAsync(new SystemException("system fail"));

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportPldns(req, "unit.test");

        // Assert
        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task ImportPldns_Run_ShouldReturn500_ForGenericException()
    {
        // Arrange - job config throws generic exception
        _blobServiceMock
            .Setup(s => s.DownloadFileAsync(_config.PldnsImportUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        _jobConfigurationServiceMock
            .Setup(s => s.ReadPldnsImportConfiguration())
            .ThrowsAsync(new Exception("boom"));

        var req = new MockHttpRequestData(_functionContext);

        // Act
        var result = await _function.ImportPldns(req, "unit.test");

        // Assert
        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    private static MemoryStream CreatePldnsWorkbookStream(bool includeTargetSheet, int addHeaderRowIndex, int addDataRowIndex)
    {
        var ms = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var workbook = workbookPart.Workbook;
            var sheets = workbook.AppendChild(new Sheets());

            // Create worksheet part
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            // First row - title
            var row1 = new Row { RowIndex = 1 };
            row1.Append(CreateInlineTextCell("A1", "Title row"));
            sheetData.Append(row1);

            // Header row at requested index
            var headerRow = new Row { RowIndex = (uint)addHeaderRowIndex };
            headerRow.Append(CreateInlineTextCell($"A{addHeaderRowIndex}", "text QAN"));
            headerRow.Append(CreateInlineTextCell($"B{addHeaderRowIndex}", "Date PLDNS list updated"));
            sheetData.Append(headerRow);

            // Data row
            var dataRow = new Row { RowIndex = (uint)addDataRowIndex };
            dataRow.Append(CreateInlineTextCell($"A{addDataRowIndex}", " QAN123 ")); 
                                                                                     
            dataRow.Append(CreateInlineTextCell($"B{addDataRowIndex}", "01/02/2023"));
            dataRow.Append(CreateNumericCell($"C{addDataRowIndex}", DateTime.ParseExact("01/02/2023", "dd/MM/yyyy", new CultureInfo("en-GB")).ToOADate().ToString(CultureInfo.InvariantCulture)));
            sheetData.Append(dataRow);

            // Add sheet referencing this worksheet
            var sheetName = includeTargetSheet ? "PLDNS V12F" : "OTHER";
            var sheetId = 1u;
            sheets.Append(new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = sheetId,
                Name = sheetName
            });

            // Save workbook
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

    private static Cell CreateNumericCell(string cellRef, string numericText)
    {
        return new Cell
        {
            CellReference = cellRef,
            CellValue = new CellValue(numericText)
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