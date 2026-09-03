using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;

namespace SFA.DAS.AODP.Jobs.Helpers;

public static class BlobPathParser
{
    public static (FileCategory Category, Guid? ApplicationId, Guid? MessageId, Guid? QuestionId) ParseBlobPath(string containerName, string blobPath)
    {
        var segments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        FileCategory category = FileCategory.Unknown;
        Guid? applicationId = null;
        Guid? messageId = null;
        Guid? questionId = null;

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
                category = FileCategory.MessageAttachment;
                applicationId = Guid.Parse(segments[1]);
                messageId = Guid.Parse(segments[2]);
            }
            else
            {
                // files/{applicationId}/{questionId}/{fileId}
                category = FileCategory.QuestionUpload;
                applicationId = Guid.Parse(segments[0]);
                questionId = Guid.Parse(segments[1]);
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
