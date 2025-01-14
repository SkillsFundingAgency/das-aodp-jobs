using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SFA.DAS.Configuration.AzureTableStorage;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

public static class ConfigurationExtensions
{
    public static void AddConfiguration(this IConfigurationBuilder builder)
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true);

        var configuration = builder.Build();

        builder.AddAzureTableStorage(options =>
        {
            options.ConfigurationKeys = configuration["ConfigNames"]!.Split(",");
            options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
            options.EnvironmentName = configuration["EnvironmentName"];
            options.PreFixConfigurationKeys = false;
        });
    }
}
