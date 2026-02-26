namespace SFA.DAS.AODP.Jobs.StartupExtensions;

[ExcludeFromCodeCoverage]
public static class ConfigurationExtensions
{
    public static IConfigurationRoot LoadConfiguration(this IConfiguration configuration, IServiceCollection services, bool isDevelopment)
    {
        var configBuilder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", true);

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
