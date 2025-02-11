namespace SFA.DAS.AODP.Configuration.Config
{
    public class AodpJobsConfiguration
    {
        public string? AzureWebJobsStorage { get; set; }

        public string? FUNCTIONS_WORKER_RUNTIME { get; set; }
    
        public string? DefaultConnection { get; set; }

        public string? OcpApimSubscriptionKey { get; set; }
        
        public int? DefaultPage { get; set; }

        public int? DefaultLimit { get; set; }

        public string? FundedQualificationsImportUrl { get; set; }

        public string? ArchivedFundedQualificationsImportUrl { get; set; }

        public string? ConfigurationStorageConnectionString { get; set; }

        public string? ConfigNames { get; set; }
        
        public string? Environment { get; set; }

    }
}
