namespace SFA.DAS.AODP.Common.Storage
{

    public static class BlobStoragePaths
    {
        // Containers
        public const string ContainerFiles = "files";
        public const string ContainerImportFiles = "importfilescontainer";
        public const string ContainerFundingImport = "funded-qualifications-import";
        public const string ContainerFundingOutput = "funded-qualifications-output";

        // Folders inside importfilescontainer
        public const string FolderDefundingList = "DefundingList";
        public const string FolderPldns = "Pldns";

        // Folders inside files
        public const string FolderMessages = "messages";

        // Funding import filenames
        public const string ApprovedFundingFileName = "approved.csv";
        public const string ArchivedFundingFileName = "archived.csv";

        // Funding output folder 
        public const string FundingOutputFolder = "funded-qualifications-output";
    }

}
