using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Helpers;

namespace SFA.DAS.AODP.Jobs.Functions;

public class ImportDefundingListDataFunction
{
    private readonly ILogger<ImportDefundingListDataFunction> _logger;
    private readonly AodpJobsConfiguration _config;
    private readonly IJobConfigurationService _jobConfigurationService;
    private readonly IImportRepository _repository;
    private readonly IFileProcessingService _fileProcessingService;

    public ImportDefundingListDataFunction(ILogger<ImportDefundingListDataFunction> logger,
            AodpJobsConfiguration config,
            IJobConfigurationService jobConfigurationService,
            IImportRepository repository,
            IFileProcessingService fileProcessingService)
    {
        _logger = logger;
        _config = config;
        _jobConfigurationService = jobConfigurationService;
        _repository = repository;
        _fileProcessingService = fileProcessingService;
    }

    // Todo - Merge with ImportPldnDataFunction as they are almost identical apart from the data being imported
    [Function("ImportDefundingListDataFunction")]
    public async Task<IActionResult> ImportDefundingList(
         [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/importDefundingList/{username}")]
        HttpRequestData req,
         string username = "",
         CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Function}] -> ImportDefundingList triggered by {Username}",
            nameof(ImportDefundingListDataFunction), username);

        var jobControl = await _jobConfigurationService.ReadDefundingListImportConfiguration();
        var lastJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.DefundingList.ToString());

        var fileResult = await _fileProcessingService.GetReadyFileAsync(
            FileCategory.DefundingList,
            username,
            jobControl.JobId,
            lastJobRun.Id,
            lastJobRun.StartTime,
            cancellationToken);

        if (!fileResult.IsReady)
        {
            return new OkObjectResult("Defunding List File not ready");
        }

        await using var stream = fileResult.Stream!;

        var totalImported = await ImportDefundingList(stream, cancellationToken);

        await _jobConfigurationService.UpdateJobRun(
            username,
            jobControl.JobId,
            lastJobRun.Id,
            totalImported,
            JobStatus.Completed);

        var msg = $"[{nameof(ImportDefundingListDataFunction)}] -> {totalImported} records imported.";
        _logger.LogInformation(msg);

        return new OkObjectResult(msg);
    }

    private async Task<int> ImportDefundingList(Stream stream, CancellationToken cancellationToken)
    {

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }


        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part missing.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;

        // Get target sheet
        var targetSheetName = "Approval not extended";
        var chosenSheet = workbookPart.Workbook.Sheets!
            .Cast<Sheet?>()
            .FirstOrDefault(s => string.Equals((s?.Name!.Value ?? string.Empty).Trim(), targetSheetName, StringComparison.OrdinalIgnoreCase));

        if (chosenSheet == null)
        {
            return 0;
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(chosenSheet.Id!);
        var rows = ImportHelper.GetRowsFromWorksheet(worksheetPart).ToList();
        if (rows.Count <= 1)
        {
            return 0;
        }

        // Detect header row
        var headerKeywords = new[] { "qualification", "qan", "title", "award", "guided", "sector", "route", "funding", "in scope", "comments" };
        var (headerRow, headerIndex) = ImportHelper.DetectHeaderRow(rows, sharedStrings, headerKeywords, defaultRowIndex: 6, minMatches: 2);

        // Build header map
        var headerMap = ImportHelper.BuildHeaderMap(headerRow, sharedStrings);

        // Parse data rows into items
        var items = ParseDataRows(rows, headerIndex + 1, headerMap, worksheetPart, sharedStrings);

        if (items.Count == 0)
        {
            return 0;
        }

        await _repository.BulkInsertAsync(items, cancellationToken);
        await _repository.DeleteDuplicateAsync("[dbo].[proc_DeleteDuplicateDefundingLists]", null, cancellationToken);

        return items.Count;
    }

    private static List<DefundingList> ParseDataRows(List<Row> rows, int startIndex, IDictionary<string, string> headerMap, WorksheetPart worksheetPart, SharedStringTable? sharedStrings)
    {
        var items = new List<DefundingList>();

        // normalize start index
        if (startIndex < 0) startIndex = 0;

        var localRows = rows ?? new List<Row>();
        var total = localRows.Count;
        if (total == 0 || startIndex >= total) return items;

        // resolve columns once
        string? qCol = ImportHelper.FindColumn(headerMap, "Qualification number");
        string? titleCol = ImportHelper.FindColumn(headerMap, "Title");
        string? awardingCol = ImportHelper.FindColumn(headerMap, "Awarding organisation");
        string? glhCol = ImportHelper.FindColumn(headerMap, "Guided Learning Hours");
        string? ssaCol = ImportHelper.FindColumn(headerMap, "Sector Subject Area Tier 2");
        string? routeCol = ImportHelper.FindColumn(headerMap, "Relevant route");
        string? fundingCol = ImportHelper.FindColumn(headerMap, "Funding offer");
        string? inScopeCol = ImportHelper.FindColumn(headerMap, "InScope", "In Scope");
        string? commentsCol = ImportHelper.FindColumn(headerMap, "Comments");

        for (int i = startIndex; i < total; i++)
        {
            var row = localRows[i];
            var rowIndex = row.RowIndex?.Value.ToString() ?? (i + 1).ToString();

            var qNumber = ImportHelper.GetValue(worksheetPart, rowIndex, qCol, sharedStrings);
            if (string.IsNullOrWhiteSpace(qNumber))
            {
                continue;
            }

            var title = ImportHelper.GetValue(worksheetPart, rowIndex, titleCol, sharedStrings);
            var awardingOrg = ImportHelper.GetValue(worksheetPart, rowIndex, awardingCol, sharedStrings);
            var glh = ImportHelper.GetValue(worksheetPart, rowIndex, glhCol, sharedStrings);
            var ssa = ImportHelper.GetValue(worksheetPart, rowIndex, ssaCol, sharedStrings);
            var route = ImportHelper.GetValue(worksheetPart, rowIndex, routeCol, sharedStrings);
            var fundingOffer = ImportHelper.GetValue(worksheetPart, rowIndex, fundingCol, sharedStrings);
            var inScopeStr = ImportHelper.GetValue(worksheetPart, rowIndex, inScopeCol, sharedStrings);
            var comments = ImportHelper.GetValue(worksheetPart, rowIndex, commentsCol, sharedStrings);

            var inScope = ParseInScope(inScopeStr);

            static string? ToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

            var item = new DefundingList
            {
                Qan = qNumber,
                Title = ToNull(title),
                AwardingOrganisation = ToNull(awardingOrg),
                GuidedLearningHours = ToNull(glh),
                SectorSubjectArea = ToNull(ssa),
                RelevantRoute = ToNull(route),
                FundingOffer = ToNull(fundingOffer),
                InScope = inScope,
                Comments = ToNull(comments),
                ImportDate = DateTime.UtcNow
            };
            items.Add(item);
        }

        return items;
    }

    private static bool ParseInScope(string? inScopeStr)
    {
        if (string.IsNullOrWhiteSpace(inScopeStr)) return true;
        var normalized = inScopeStr.Trim().ToLowerInvariant();
        if (normalized is "0" or "false" or "no" or "n" or "excluded") return false;
        if (normalized is "1" or "true" or "yes" or "y" or "included") return true;
        if (bool.TryParse(inScopeStr, out var b)) return b;
        if (int.TryParse(inScopeStr, out var i)) return i != 0;
        return true;
    }

}