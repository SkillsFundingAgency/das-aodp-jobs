using SFA.DAS.AODP.Infrastructure.Authentication;
using QaaApiAuthenticationHandler = SFA.DAS.AODP.Jobs.Client.QaaApiAuthenticationHandler;

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

        if (configuration["EnvironmentName"]! == "LOCAL")
        {
            services.AddTransient<IQaaApiClient, StubQaaClient>();
        }
        else
        {
            services.AddHttpClient<IQaaApiClient, QaaApiClient>((sp, client) =>
                {
                    var qaaApiConfiguration = sp.GetRequiredService<IOptions<QaaApiConfiguration>>().Value;

                    client.BaseAddress = new Uri(qaaApiConfiguration.BaseUrl);
                })
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
        }

        services.AddTransient<IQaaQualificationImportService, QaaQualificationImportService>();
        services.AddTransient<IQaaRepository, QaaRepository>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }
}

public class StubQaaClient : IQaaApiClient
{
    public async Task<IList<QaaQualificationResponse>> GetQualificationsAsync(CancellationToken cancellationToken)
    {
        return new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = "123456",
                AwardingBody = "Ascend Learning",
                DiplomaTitle = "Level 3 Diploma in Business Administration",
                SsaTier1 = "1",
                SsaTier2 = "1",
                StartDateOfQualification = new DateTime(2020, 1, 1),
                LastDateForRegistrations = new DateTime(2025, 12, 31),
                LastDateForCertifications = new DateTime(2026, 12, 31),
                AwardStatus = "Active",
                DiscontinuedDate = null
            },
            new()
            {
                AimCode = "456789",
                AwardingBody = "Ascend Learning",
                DiplomaTitle = "Level 3 Diploma in Construction",
                SsaTier1 = "1",
                SsaTier2 = "4",
                StartDateOfQualification = new DateTime(2020, 1, 1),
                LastDateForRegistrations = new DateTime(2025, 12, 31),
                LastDateForCertifications = new DateTime(2026, 12, 31),
                AwardStatus = "Active",
                DiscontinuedDate = null
            }
        };
    }
}