namespace SFA.DAS.AODP.Jobs.Models.Jobs
{
    public static class ImportStoragePaths
    {
        public const string PldnsFolder = "pldns";
        public const string PldnsFileName = "Pldns.xlsx";
        public static string PldnsFileLogicalPath => $"{PldnsFolder}/{PldnsFileName}";

        public const string DefundingListFolder = "defunding-list";
        public const string DefundingListFileName = "DefundingList.xlsx";
        
        public static string DefundingListFileLogicalPath => $"{DefundingListFolder}/{DefundingListFileName}";

        public const string FundingFolder = "funding-import";
        public const string ArchivedFundingFileName = "Archived.csv";
        public const string ApprovedFundingFileName = "Approved.csv";

        
        public static string ApprovedFundingFileLogicalPath => $"{FundingFolder}/{ApprovedFundingFileName}";

        public static string ArchivedFundingFileLogicalPath => $"{FundingFolder}/{ArchivedFundingFileName}";

    }
}
