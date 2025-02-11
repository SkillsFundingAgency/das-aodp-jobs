using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Configuration.AzureTableStorage;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IConfigurationRoot LoadConfiguration(this IConfiguration configuration, IServiceCollection services, bool isDevelopment)
    {
        var configBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables();

        var mergedConfig = configBuilder
            .AddAzureTableStorage(options =>
            {
                options.ConfigurationKeys = configuration["ConfigNames"]?.Split(",");
                options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
                options.EnvironmentName = configuration["EnvironmentName"];
                options.PreFixConfigurationKeys = false;
            })
            .Build();

        return new ConfigurationBuilder()
            .AddConfiguration(mergedConfig)
            .AddAzureTableStorageConfiguration(
                "UseDevelopmentStorage=true",
                "SFA.DAS.AODP.Jobs",
                "LOCAL",
                "1.0")
            .Build();
    }
}
