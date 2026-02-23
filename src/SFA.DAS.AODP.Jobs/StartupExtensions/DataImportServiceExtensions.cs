using SFA.DAS.AODP.Infrastructure.Authentication;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register data import related services into the DI container.
/// </summary>
public static class DataImportServiceExtensions
{
    /// <summary>
    /// Top level extension to add in all the data import services into DI container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Represents the key/value pairs of application configuration properties.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDataImportServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddQaaDataImportServices(configuration);

        return services;
    }

    /// <summary>
    /// Adds Qaa related services into the DI container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The key/value pairs for the configuration.</param>
    /// <returns>The updated service collection.</returns>
    private static IServiceCollection AddQaaDataImportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<QaaApiConfiguration>().Bind(configuration.GetSection(QaaApiConfiguration.SectionName));
        services.AddTransient<QaaApiAuthenticationHandler>();

            services.AddHttpClient<IQaaApiClient, QaaApiClient>((sp, client) =>
                {
                    var qaaApiConfiguration = sp.GetRequiredService<IOptions<QaaApiConfiguration>>().Value;

                    client.BaseAddress = new Uri(qaaApiConfiguration.BaseUrl);
                })
                .RedactLoggedHeaders(["Authorization"])
                .AddHttpMessageHandler<QaaApiAuthenticationHandler>()
                .AddStandardResilienceHandler();

            services.AddKeyedSingleton<TokenCredential>("QaaApi", (sp, _) =>
            {
                var qaaApiConfiguration = sp.GetRequiredService<IOptions<QaaApiConfiguration>>().Value;
                return new ClientSecretCredential(
                    tenantId: qaaApiConfiguration.Authentication.TenantId,
                    clientId: qaaApiConfiguration.Authentication.ClientId,
                    clientSecret: qaaApiConfiguration.Authentication.ClientSecret
                );
            });
        

        services.AddTransient<IQaaQualificationImportService, QaaQualificationImportService>();
        services.AddTransient<IQaaRepository, QaaRepository>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }
}