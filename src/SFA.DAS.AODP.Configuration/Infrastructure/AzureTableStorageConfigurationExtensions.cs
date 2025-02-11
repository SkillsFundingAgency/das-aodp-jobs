using Microsoft.Extensions.Configuration;
using SFA.DAS.AODP.Configuration.Infrastructure;

namespace SFA.DAS.AODP.Jobs.StartupExtensions
{
    public static class AzureTableStorageConfigurationExtensions
    {
        public static IConfigurationBuilder AddAzureTableStorageConfiguration(this IConfigurationBuilder builder, string connection, string appName, string environment, string version)
        {
            return builder.Add(new AzureTableStorageConfigurationSource(connection, appName, environment, version));
        }
    }
}
