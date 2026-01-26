using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Moq;
using SFA.DAS.AODP.Jobs.Helpers;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

public class ImportHelperTests
{
    private static readonly string[] s_notPresentKeywords = new[] { "notpresent" };
    private static readonly string[] s_keywordKeywords = new[] { "keyword" };

    [Fact]
    public void GetCellText_NullCell_ReturnsEmpty()
    {
        // Arrange
        Cell? cell = null;
        SharedStringTable? sst = null;

        // Act
        var result = ImportHelper.GetCellText(cell!, sst);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetCellText_InlineString_ReturnsInnerText()
    {
        // Arrange
        var cell = new Cell
        {
            DataType = new EnumValue<CellValues>(CellValues.InlineString),
            InlineString = new InlineString(new Text("inline"))
        };

        // Act
        var result = ImportHelper.GetCellText(cell, null);

        // Assert
        Assert.Equal("inline", result);
    }

    [Fact]
    public void GetCellText_NoDataType_ReturnsCellValue()
    {
        // Arrange
        var cell = new Cell { CellValue = new CellValue("value"), DataType = null };

        // Act
        var result = ImportHelper.GetCellText(cell, null);

        // Assert
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetCellText_SharedString_NoSharedStringTable_ReturnsRawValue()
    {
        // Arrange
        var cell = new Cell { DataType = new EnumValue<CellValues>(CellValues.SharedString), CellValue = new CellValue("0") };

        // Act
        var result = ImportHelper.GetCellText(cell, null);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void GetCellText_SharedString_WithInnerText_ReturnsSharedStringInnerText()
    {
        // Arrange
        var sst = new SharedStringTable();
        sst.AppendChild(new SharedStringItem(new Text("shared-value")));
        var cell = new Cell { DataType = new EnumValue<CellValues>(CellValues.SharedString), CellValue = new CellValue("0") };

        // Act
        var result = ImportHelper.GetCellText(cell, sst);

        // Assert
        Assert.Equal("shared-value", result);
    }

    [Fact]
    public void GetCellText_SharedString_WithRuns_ReturnsRunTextWhenNoInnerText()
    {
        // Arrange
        var sst = new SharedStringTable();
        var ssi = new SharedStringItem();
        var run = new Run(new Text("run-text"));
        ssi.AppendChild(run);
        sst.AppendChild(ssi);
        var cell = new Cell { DataType = new EnumValue<CellValues>(CellValues.SharedString), CellValue = new CellValue("0") };

        // Act
        var result = ImportHelper.GetCellText(cell, sst);

        // Assert
        Assert.Equal("run-text", result);
    }

    [Fact]
    public void GetCellText_SharedString_InvalidIndex_ReturnsEmptyString()
    {
        // Arrange
        var sst = new SharedStringTable();
        var cell = new Cell { DataType = new EnumValue<CellValues>(CellValues.SharedString), CellValue = new CellValue("5") };

        // Act
        var result = ImportHelper.GetCellText(cell, sst);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetCellText_Boolean_MapsCorrectly()
    {
        // Arrange
        var trueCell = new Cell { DataType = new EnumValue<CellValues>(CellValues.Boolean), CellValue = new CellValue("1") };
        var falseCell = new Cell { DataType = new EnumValue<CellValues>(CellValues.Boolean), CellValue = new CellValue("0") };
        var otherCell = new Cell { DataType = new EnumValue<CellValues>(CellValues.Boolean), CellValue = new CellValue("2") };

        // Act
        var trueResult = ImportHelper.GetCellText(trueCell, null);
        var falseResult = ImportHelper.GetCellText(falseCell, null);
        var otherResult = ImportHelper.GetCellText(otherCell, null);

        // Assert
        Assert.Equal("TRUE", trueResult);
        Assert.Equal("FALSE", falseResult);
        Assert.Equal("2", otherResult);
    }

    [Fact]
    public void GetColumnName_VariousInputs()
    {
        // Arrange & Act
        var nullResult = ImportHelper.GetColumnName(null);
        var emptyResult = ImportHelper.GetColumnName(string.Empty);
        var aResult = ImportHelper.GetColumnName("A1");
        var bcResult = ImportHelper.GetColumnName("BC23");
        var numericResult = ImportHelper.GetColumnName("123");

        // Assert
        Assert.Null(nullResult);
        Assert.Null(emptyResult);
        Assert.Equal("A", aResult);
        Assert.Equal("BC", bcResult);
        Assert.True(string.IsNullOrEmpty(numericResult));
    }

    [Fact]
    public void FindColumn_WithExactAndContainsMatches_WorksAsExpected()
    {
        // Arrange
        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "Qualification Title",
            ["B"] = "Provider",
            ["C"] = "Other"
        };

        // Act
        var providerMatch = ImportHelper.FindColumn(headerMap, " provider ");
        var titleMatch = ImportHelper.FindColumn(headerMap, "title");
        var nopeMatch = ImportHelper.FindColumn(headerMap, "nope");
        var nullCall = ImportHelper.FindColumn(headerMap);

        // Assert
        Assert.Equal("B", providerMatch);
        Assert.Equal("A", titleMatch);
        Assert.Null(nopeMatch);
        Assert.Null(nullCall);
    }

    [Fact]
    public void FindColumn_WithMockedDictionary_ReturnsKey()
    {
        // Arrange
        var real = new Dictionary<string, string> { ["X"] = "HeaderName" };
        var mock = new Mock<IDictionary<string, string>>();
        mock.Setup(m => m.GetEnumerator()).Returns(() => real.GetEnumerator());
        mock.Setup(m => m.Keys).Returns(real.Keys);
        mock.Setup(m => m.Values).Returns(real.Values);
        mock.Setup(m => m.Count).Returns(real.Count);

        // Act
        var result = ImportHelper.FindColumn(mock.Object, "headername");

        // Assert
        Assert.Equal("X", result);
    }

    [Fact]
    public void BuildHeaderMap_BuildsFromRow_CorrectKeysAndValues()
    {
        // Arrange
        var row = new Row();
        row.Append(new Cell { CellReference = "A1", CellValue = new CellValue("First") });
        row.Append(new Cell { CellReference = "B1", CellValue = new CellValue("Second") });

        // Act
        var map = ImportHelper.BuildHeaderMap(row, null);

        // Assert
        Assert.Equal(2, map.Count);
        Assert.Equal("First", map["A"]);
        Assert.Equal("Second", map["B"]);
    }

    [Fact]
    public void GetRowsFromWorksheet_YieldsRows_WhenSheetDataPresent()
    {
        // Arrange
        using var mem = new MemoryStream();
        using var spreadsheet = SpreadsheetDocument.Create(mem, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheet.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        var row = new Row();
        row.Append(new Cell { CellReference = "A1", CellValue = new CellValue("x") });
        sheetData.Append(row);
        worksheetPart.Worksheet = new Worksheet(sheetData);

        // Act
        var rows = ImportHelper.GetRowsFromWorksheet(worksheetPart).ToList();

        // Assert
        Assert.Single(rows);
        Assert.Equal("A1", rows[0].Elements<Cell>().First().CellReference?.Value);
    }

    [Fact]
    public void DetectHeaderRow_DefaultAndDetectedBehavior()
    {
        // Arrange
        var r0 = new Row();
        var r1 = new Row();
        r1.Append(new Cell { CellValue = new CellValue("DefaultHeader") });
        var r2 = new Row(); // will contain matching header
        r2.Append(new Cell { CellValue = new CellValue("contains keyword") });

        var rows = new List<Row> { r0, r1, r2 };

        // Act
        var (headerRow1, idx1) = ImportHelper.DetectHeaderRow(rows, null, s_notPresentKeywords, defaultRowIndex: 1, minMatches: 1);
        var (headerRow2, idx2) = ImportHelper.DetectHeaderRow(rows, null, s_keywordKeywords, defaultRowIndex: 1, minMatches: 1);

        // Assert
        Assert.Equal(1, idx1);
        Assert.Equal(r1, headerRow1);

        Assert.Equal(2, idx2);
        Assert.Equal(r2, headerRow2);
    }

    [Fact]
    public void GetCellTextByColumn_And_GetValue_WorkWithWorksheetPart()
    {
        // Arrange
        using var mem = new MemoryStream();
        using var spreadsheet = SpreadsheetDocument.Create(mem, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheet.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();

        // Create a row 2 with a cell B2
        var row = new Row { RowIndex = 2 };
        var cell = new Cell { CellReference = "B2", CellValue = new CellValue(" cell value ") };
        row.Append(cell);
        sheetData.Append(row);
        worksheetPart.Worksheet = new Worksheet(sheetData);

        // Act
        var got = ImportHelper.GetCellTextByColumn(worksheetPart, "2", "B", null);
        var value = ImportHelper.GetValue(worksheetPart, "2", "B", null);
        var missingRow = ImportHelper.GetCellTextByColumn(worksheetPart, "3", "A", null);
        var emptyRowIndex = ImportHelper.GetCellTextByColumn(worksheetPart, "", "B", null);
        var emptyColumn = ImportHelper.GetValue(worksheetPart, "2", "", null);

        // Assert
        Assert.Equal("cell value", got);
        Assert.Equal("cell value", value);
        Assert.Equal(string.Empty, missingRow);
        Assert.Equal(string.Empty, emptyRowIndex);
        Assert.Equal(string.Empty, emptyColumn);
    }
}
