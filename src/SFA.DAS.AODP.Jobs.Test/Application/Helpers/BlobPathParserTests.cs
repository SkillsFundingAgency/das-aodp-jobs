using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

public class BlobPathParserTests
{
    [Fact]
    public void ParseBlobPath_ShouldParseQuestionUpload_ForValidApplicationAndQuestionGuids()
    {
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var result = BlobPathParser.ParseBlobPath("files", $"{applicationId}/{questionId}/evidence.pdf");

        Assert.Equal(FileCategory.QuestionUpload, result.Category);
        Assert.Equal(applicationId, result.ApplicationId);
        Assert.Equal(questionId, result.QuestionId);
        Assert.Null(result.MessageId);
    }

    [Fact]
    public void ParseBlobPath_ShouldParseMessageAttachment_ForValidApplicationAndMessageGuids()
    {
        var applicationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var result = BlobPathParser.ParseBlobPath("files", $"messages/{applicationId}/{messageId}/attachment.pdf");

        Assert.Equal(FileCategory.MessageAttachment, result.Category);
        Assert.Equal(applicationId, result.ApplicationId);
        Assert.Equal(messageId, result.MessageId);
        Assert.Null(result.QuestionId);
    }

    [Theory]
    [InlineData("file7.docx")]
    [InlineData("another name.csv")]
    [InlineData("Pldns/somefile.xlsx")]
    public void ParseBlobPath_ShouldReturnUnknown_ForFilesBlobsThatDoNotFitEitherShape(string blobPath)
    {
        var result = BlobPathParser.ParseBlobPath("files", blobPath);

        Assert.Equal(FileCategory.Unknown, result.Category);
        Assert.Null(result.ApplicationId);
        Assert.Null(result.QuestionId);
        Assert.Null(result.MessageId);
    }

    [Fact]
    public void ParseBlobPath_ShouldReturnUnknown_WhenQuestionUploadSecondSegmentIsNotAGuid()
    {
        var result = BlobPathParser.ParseBlobPath("files", $"{Guid.NewGuid()}/not-a-guid/file.pdf");

        Assert.Equal(FileCategory.Unknown, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldReturnUnknown_WhenMessagesPathIsMissingSegments()
    {
        var result = BlobPathParser.ParseBlobPath("files", $"messages/{Guid.NewGuid()}");

        Assert.Equal(FileCategory.Unknown, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldReturnUnknown_ForEmptyBlobPath()
    {
        var result = BlobPathParser.ParseBlobPath("files", string.Empty);

        Assert.Equal(FileCategory.Unknown, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldParseDefundingList_ForImportFilesContainer()
    {
        var result = BlobPathParser.ParseBlobPath("importfilescontainer", "DefundingList/" + Guid.NewGuid());

        Assert.Equal(FileCategory.DefundingList, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldParsePldns_ForImportFilesContainer()
    {
        var result = BlobPathParser.ParseBlobPath("importfilescontainer", "Pldns/" + Guid.NewGuid());

        Assert.Equal(FileCategory.Pldns, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldParseApprovedFunding_ForFundingImportContainer()
    {
        var result = BlobPathParser.ParseBlobPath("funded-qualifications-import", "approved.csv");

        Assert.Equal(FileCategory.ApprovedFunding, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldParseArchivedFunding_ForFundingImportContainer()
    {
        var result = BlobPathParser.ParseBlobPath("funded-qualifications-import", "archived.csv");

        Assert.Equal(FileCategory.ArchivedFunding, result.Category);
    }

    [Fact]
    public void ParseBlobPath_ShouldReturnUnknown_ForUnrecognisedContainer()
    {
        var result = BlobPathParser.ParseBlobPath("some-other-container", "whatever.txt");

        Assert.Equal(FileCategory.Unknown, result.Category);
    }
}
