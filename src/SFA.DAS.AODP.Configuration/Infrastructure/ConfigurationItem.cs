using Microsoft.Azure.Cosmos.Table;

namespace SFA.DAS.AODP.Configuration.Infrastructure
{
    public class ConfigurationItem : TableEntity
    {
        public string Data { get; set; }
    }
}
