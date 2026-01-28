namespace SFA.DAS.AODP.Models.Config
{
    public class AodpJobsConfiguration
    {
        public string? AzureWebJobsStorage { get; set; }

        public string? FUNCTIONS_WORKER_RUNTIME { get; set; }
    
        public string? DbConnectionString { get; set; }

        public string? OcpApimSubscriptionKey { get; set; }
        
        public int DefaultImportPage { get; set; }

        public int DefaultImportLimit { get; set; }

        public string? FundedQualificationsImportUrl { get; set; }

        public string? ArchivedFundedQualificationsImportUrl { get; set; }

        public string? PldnsImportUrl { get; set; }

        public string? DefundingListImportUrl { get; set; }

        public string? ConfigurationStorageConnectionString { get; set; }

        public string? ConfigNames { get; set; }

        public string? Environment { get; set; }

        public string? FunctionAppBaseUrl { get; set; }
        public string? FunctionHostKey { get; set; }
    }
}
