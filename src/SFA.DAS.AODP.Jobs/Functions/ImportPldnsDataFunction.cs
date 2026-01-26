using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Helpers;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Config;
using System.Globalization;
using System.Text;

namespace SFA.DAS.AODP.Jobs.Functions;

public class ImportPldnsDataFunction
{
    private readonly ILogger<ImportPldnsDataFunction> _logger;
    private readonly AodpJobsConfiguration _config;
    private readonly IJobConfigurationService _jobConfigurationService;
    private readonly IImportRepository _repository;
    private readonly IBlobStorageFileService _blobStorageFileService;
    private const int BatchSize = 3000;

    public ImportPldnsDataFunction(ILogger<ImportPldnsDataFunction> logger,
            AodpJobsConfiguration config,
            IJobConfigurationService jobConfigurationService,
            IImportRepository repository,
            IBlobStorageFileService blobStorageFileService)
    {
        _logger = logger;
        _config = config;
        _jobConfigurationService = jobConfigurationService;
        _repository = repository;
        _blobStorageFileService = blobStorageFileService;
    }

    // Todo : Merge with ImportDefundingListDataFunction as they are almost identical apart from the data being imported
    [Function("ImportPldnsDataFunction")]
    public async Task<IActionResult> ImportPldns(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/importPldns/{username}")] HttpRequestData req, string username = "", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Function}] -> ImportPldns triggered by {Username}", nameof(ImportPldnsDataFunction), username);
        try
        {
            var totalImported = await ImportPldns(cancellationToken);

            var jobControl = await _jobConfigurationService.ReadPldnsImportConfiguration();

            var lastJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.Pldns.ToString());

            await _jobConfigurationService.UpdateJobRun(username, jobControl.JobId, lastJobRun.Id, totalImported, JobStatus.Completed);

            var msg = $"[{nameof(ImportPldnsDataFunction)}] -> {totalImported} records imported.";
            _logger.LogInformation("[{Function}] -> {TotalImported} records imported.", nameof(ImportPldnsDataFunction), totalImported);
            return new OkObjectResult(msg);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[{Function}] -> ImportPldns unexpected api exception: {Message}", nameof(ImportPldnsDataFunction), ex.Message);
            return new StatusCodeResult((int)ex.StatusCode);
        }
        catch (SystemException ex)
        {
            _logger.LogError(ex, "[{Function}] -> ImportPldns unexpected system exception: {Message}", nameof(ImportPldnsDataFunction), ex.Message);
            return new StatusCodeResult(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Function}] -> ImportPldns failed: {Message}", nameof(ImportPldnsDataFunction), ex.Message);
            return new StatusCodeResult(500);
        }
    }

    private async Task<int> ImportPldns(CancellationToken cancellationToken)
    {
        string? importFileUrl = _config.PldnsImportUrl;
        await using var ms = await _blobStorageFileService.DownloadFileAsync(importFileUrl!, cancellationToken);
        ms.Position = 0;

        using var document = SpreadsheetDocument.Open(ms, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part missing.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

        var sheet = FindSheet(workbookPart, "PLDNS V12F");
        if (sheet == null)
        {
            _logger.LogWarning("[{Function}] -> ImportPldns - Sheet {SheetName} not found", nameof(ImportPldnsDataFunction), "PLDNS V12F");
            return 0;
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
        if (sheetData == null)
        {
            _logger.LogWarning("[{Function}] -> ImportPldns - Sheet data is null for sheet {SheetName}", nameof(ImportPldnsDataFunction), "PLDNS V12F");
            return 0;
        }

        var rows = sheetData.Elements<Row>().ToList();
        if (rows.Count <= 1)
        {
            return 0;
        }

        var headerKeywords = new[] {
                "text qan","list updated","note",
                "pldns 14-16","pldns 16-19","pldns local flex",
                "legal entitlement","digital entitlement","esf l3/l4",
                "pldns loans","lifelong learning entitlement","level 3 free courses",
                "pldns cof","start date"
            };

        var (headerRow, headerIndex) = ImportHelper.DetectHeaderRow(rows, sharedStrings, headerKeywords, defaultRowIndex: 1, minMatches: 1);

        var headerMap = ImportHelper.BuildHeaderMap(headerRow, sharedStrings);
        var columns = MapColumns(headerMap);

        var culture = new CultureInfo("en-GB");
        var dateFormats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd MMM yyyy" };

        var items = ParseRowsToEntities(rows, headerIndex + 1, sharedStrings, columns, culture, dateFormats);

        if (items.Count == 0)
        {
            _logger.LogWarning("[{Function}] -> ImportPldns - No records available to import.", nameof(ImportPldnsDataFunction));
            return 0;
        }

        var totalImported = await InsertBatchesAsync(items, cancellationToken);

        await _repository.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicatePldns]", null, cancellationToken);

        return totalImported;
    }

    private static Sheet? FindSheet(WorkbookPart workbookPart, string targetSheetName)
    {
        return workbookPart.Workbook.Sheets!
            .Cast<Sheet?>()
            .FirstOrDefault(s => string.Equals((s?.Name!.Value ?? string.Empty).Trim(), targetSheetName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record ColumnNames(
        string? Qan,
        string? ListUpdated,
        string? Note,
        string? P14To16,
        string? P14To16Note,
        string? P16To19,
        string? P16To19Note,
        string? LocalFlex,
        string? LocalFlexNote,
        string? LegalL2L3,
        string? LegalL2L3Note,
        string? LegalEngMaths,
        string? LegalEngMathsNote,
        string? Digital,
        string? DigitalNote,
        string? Esf,
        string? EsfNote,
        string? Loans,
        string? LoansNote,
        string? Lle,
        string? LleNote,
        string? Fcfj,
        string? FcfjNote,
        string? Cof,
        string? CofNote,
        string? StartDate,
        string? StartDateNote
    );

    private static ColumnNames MapColumns(IDictionary<string, string> headerMap)
    {
        return new ColumnNames(
            ImportHelper.FindColumn(headerMap, "text QAN"),
            ImportHelper.FindColumn(headerMap, "Date PLDNS list updated", "list updated"),
            ImportHelper.FindColumn(headerMap, "NOTE", "Notes"),
            ImportHelper.FindColumn(headerMap, "PLDNS 14-16"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS 14-16"),
            ImportHelper.FindColumn(headerMap, "PLDNS 16-19"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS 16-19"),
            ImportHelper.FindColumn(headerMap, "PLDNS Local flex"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Local flex"),
            ImportHelper.FindColumn(headerMap, "PLDNS Legal entitlement L2/L3"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Legal entitlement L2/L3"),
            ImportHelper.FindColumn(headerMap, "PLDNS Legal entitlement Eng/Maths"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Legal entitlement Eng/Maths"),
            ImportHelper.FindColumn(headerMap, "PLDNS Digital entitlement"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Digital entitlement"),
            ImportHelper.FindColumn(headerMap, "PLDNS ESF L3/L4"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS ESF L3/L4"),
            ImportHelper.FindColumn(headerMap, "PLDNS Loans"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Loans"),
            ImportHelper.FindColumn(headerMap, "PLDNS Lifelong Learning Entitlement"),
            ImportHelper.FindColumn(headerMap, "NOTES PLDNS Lifelong Learning Entitlement"),
            ImportHelper.FindColumn(headerMap, "PLDNS Level 3 Free Courses for Jobs"),
            ImportHelper.FindColumn(headerMap, "Level 3 Free Courses for Jobs (Previously known as National skills fund L3 extended entitlement)"),
            ImportHelper.FindColumn(headerMap, "PLDNS CoF"),
            ImportHelper.FindColumn(headerMap, "NOTES  PLDNS CoF"),
            ImportHelper.FindColumn(headerMap, "Start date"),
            ImportHelper.FindColumn(headerMap, "NOTES Start date")
        );
    }

    private static List<Pldns> ParseRowsToEntities(
        List<Row> rows,
        int startIndex,
        SharedStringTable? sharedStrings,
        ColumnNames columns,
        CultureInfo culture,
        string[] dateFormats)
    {
        var items = new List<Pldns>();

        for (int i = startIndex; i < rows.Count; i++)
        {
            var row = rows[i];

            var cellMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PopulateCellMap(row.Elements<Cell>(), sharedStrings, cellMap);

            if (string.IsNullOrWhiteSpace(columns.Qan))
                break;

            if (!cellMap.TryGetValue(columns.Qan, out var qNumber) || string.IsNullOrWhiteSpace(qNumber))
                continue;

            var item = new Pldns
            {
                Qan = qNumber!.Trim(),

                ListUpdatedDate = TryParseDate(GetValue(cellMap, columns.ListUpdated), culture, dateFormats),
                Notes = GetValue(cellMap, columns.Note)?.Trim(),

                Pldns14To16 = TryParseDate(GetValue(cellMap, columns.P14To16), culture, dateFormats),
                Pldns14To16Note = GetValue(cellMap, columns.P14To16Note)?.Trim(),

                Pldns16To19 = TryParseDate(GetValue(cellMap, columns.P16To19), culture, dateFormats),
                Pldns16To19Note = GetValue(cellMap, columns.P16To19Note)?.Trim(),

                LocalFlex = TryParseDate(GetValue(cellMap, columns.LocalFlex), culture, dateFormats),
                LocalFlexNote = GetValue(cellMap, columns.LocalFlexNote)?.Trim(),

                LegalEntitlementL2L3 = TryParseDate(GetValue(cellMap, columns.LegalL2L3), culture, dateFormats),
                LegalEntitlementL2L3Note = GetValue(cellMap, columns.LegalL2L3Note)?.Trim(),

                LegalEntitlementEngMaths = TryParseDate(GetValue(cellMap, columns.LegalEngMaths), culture, dateFormats),
                LegalEntitlementEngMathsNote = GetValue(cellMap, columns.LegalEngMathsNote)?.Trim(),

                DigitalEntitlement = TryParseDate(GetValue(cellMap, columns.Digital), culture, dateFormats),
                DigitalEntitlementNote = GetValue(cellMap, columns.DigitalNote)?.Trim(),

                EsfL3L4 = TryParseDate(GetValue(cellMap, columns.Esf), culture, dateFormats),
                EsfL3L4Note = GetValue(cellMap, columns.EsfNote)?.Trim(),

                Loans = TryParseDate(GetValue(cellMap, columns.Loans), culture, dateFormats),
                LoansNote = GetValue(cellMap, columns.LoansNote)?.Trim(),

                LifelongLearning = TryParseDate(GetValue(cellMap, columns.Lle), culture, dateFormats),
                LifelongLearningNote = GetValue(cellMap, columns.LleNote)?.Trim(),

                Level3FCoursesForJobs = TryParseDate(GetValue(cellMap, columns.Fcfj), culture, dateFormats),
                Level3FCoursesForJobsNote = GetValue(cellMap, columns.FcfjNote)?.Trim(),

                Cof = TryParseDate(GetValue(cellMap, columns.Cof), culture, dateFormats),
                CofNote = GetValue(cellMap, columns.CofNote)?.Trim(),

                StartDate = TryParseDate(GetValue(cellMap, columns.StartDate), culture, dateFormats),
                StartDateNote = GetValue(cellMap, columns.StartDateNote)?.Trim(),

                ImportDate = DateTime.UtcNow
            };

            items.Add(item);
        }

        return items;
    }

    private async Task<int> InsertBatchesAsync(List<Pldns> items, CancellationToken cancellationToken)
    {
        var totalImported = 0;
        var batches = (int)Math.Ceiling(items.Count / (double)BatchSize);
        for (var batch = 0; batch < batches; batch++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchItems = items.Skip(batch * BatchSize).Take(BatchSize).ToList();
            await _repository.BulkInsertAsync(batchItems, cancellationToken);
            _logger.LogInformation("[{Function}] -> Inserted batch {BatchNumber} with {BatchCount} records.", nameof(ImportPldnsDataFunction), batch + 1, batchItems.Count);
            totalImported += batchItems.Count;
        }
        return totalImported;
    }

    private static string? GetValue(Dictionary<string, string> map, string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return null;
        }

        if (map.TryGetValue(column!, out var v))
        {
            return v;
        }

        return null;
    }

    private static DateTime? TryParseDate(string? txt, CultureInfo culture, string[] formats)
    {
        if (string.IsNullOrWhiteSpace(txt)) return null;
        txt = txt!.Trim();
        if (DateTime.TryParse(txt, culture, DateTimeStyles.None, out var dt)) return dt.Date;
        if (DateTime.TryParseExact(txt, formats, culture, DateTimeStyles.None, out dt)) return dt.Date;
        if (double.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
        {
            return DateTime.FromOADate(oa);
        }
        return null;
    }

    private static void PopulateCellMap(IEnumerable<Cell> rowCells, SharedStringTable? sharedStrings, Dictionary<string, string> cellMap)
    {
        foreach (var cell in rowCells)
        {
            var col = ImportHelper.GetColumnName(cell.CellReference?.Value);
            if (string.IsNullOrWhiteSpace(col)) continue;
            var text = ImportHelper.GetCellText(cell, sharedStrings)?.Trim() ?? string.Empty;
            if (!cellMap.ContainsKey(col)) cellMap[col] = text;
        }
    }
}
