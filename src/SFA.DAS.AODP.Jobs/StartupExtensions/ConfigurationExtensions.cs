using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Configuration.AzureTableStorage;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IConfigurationRoot LoadConfiguration(this IConfiguration configuration, IServiceCollection services, bool isDevelopment)
    {
        var configBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", true)
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .AddEnvironmentVariables();

        if (isDevelopment)
        {
            configBuilder.AddJsonFile("appsettings.json");
        }

        configBuilder
            .AddAzureTableStorage(options =>
            {
                options.ConfigurationKeys = configuration["ConfigNames"]?.Split(",");
                options.StorageConnectionString = configuration["ConfigurationStorageConnectionString"];
                options.EnvironmentName = configuration["EnvironmentName"];
                options.PreFixConfigurationKeys = false;
            });

        var configurationRoot = configBuilder.Build();
        return configurationRoot;
    }
}
