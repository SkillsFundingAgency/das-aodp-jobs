using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;

namespace SFA.DAS.AODP.Jobs.Helpers;

public static class BlobPathParser
{
    /**
     * Never throws — a path that doesn't fit any known shape (wrong segment count, a segment
     * that isn't a real GUID where one's expected, an unrecognised container) just comes back
     * as FileCategory.Unknown, the same as any other unrecognised container. Blob storage holds
     * whatever gets dropped into it, so a caller enumerating a container has to expect stray,
     * malformed, or hand-placed files alongside the ones the app itself wrote.
     * */
    public static (FileCategory Category, Guid? ApplicationId, Guid? MessageId, Guid? QuestionId) ParseBlobPath(string containerName, string blobPath)
    {
        var segments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        FileCategory category = FileCategory.Unknown;
        Guid? applicationId = null;
        Guid? messageId = null;
        Guid? questionId = null;

        if (segments.Length == 0)
        {
            return (category, applicationId, messageId, questionId);
        }

        // importfilescontainer/DefundingList/{fileId}
        if (containerName.Equals(BlobStoragePaths.ContainerImportFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderDefundingList, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.DefundingList;
            }
            else if (segments[0].Equals(BlobStoragePaths.FolderPldns, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.Pldns;
            }
        }
        // files/messages/{appId}/{messageId}/{fileId}
        else if (containerName.Equals(BlobStoragePaths.ContainerFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderMessages, StringComparison.OrdinalIgnoreCase))
            {
                if (segments.Length >= 3
                    && Guid.TryParse(segments[1], out var parsedAppId)
                    && Guid.TryParse(segments[2], out var parsedMessageId))
                {
                    category = FileCategory.MessageAttachment;
                    applicationId = parsedAppId;
                    messageId = parsedMessageId;
                }
            }
            else
            {
                // files/{applicationId}/{questionId}/{fileId}
                if (segments.Length >= 2
                    && Guid.TryParse(segments[0], out var parsedAppId)
                    && Guid.TryParse(segments[1], out var parsedQuestionId))
                {
                    category = FileCategory.QuestionUpload;
                    applicationId = parsedAppId;
                    questionId = parsedQuestionId;
                }
            }
        }
        // funded-qualifications-import/approved.csv or archived.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingImport, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.ApprovedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ApprovedFunding;
            }
            else if (segments[0].Equals(BlobStoragePaths.ArchivedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ArchivedFunding;
            }
        }
        // funded-qualifications-output/{date}-AOdPApprovedOutputFile.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingOutput, StringComparison.OrdinalIgnoreCase))
        {
            category = FileCategory.FundingOutput;
        }

        return (category, applicationId, messageId, questionId);
    }
}
