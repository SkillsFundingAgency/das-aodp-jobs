using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RestEase;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Infrastructure.Repositories;
using SFA.DAS.AODP.Infrastructure.Services;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

[ExcludeFromCodeCoverage]
public static class AddServiceRegistrationsExtension
{
    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration)
    {

        if (!configuration.GetSection(nameof(AodpJobsConfiguration)).GetChildren().Any())
        {
            throw new ArgumentException(
                "Cannot find AodpJobsConfiguration in configuration. Please add a section called AodpJobsConfiguration with connection, default page and default limit properties.");
        }
        services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configuration));

        services.Configure<AodpJobsConfiguration>(configuration.GetSection(nameof(AodpJobsConfiguration)));
        services.AddSingleton<AodpJobsConfiguration>(sp =>
            sp.GetRequiredService<IOptions<AodpJobsConfiguration>>().Value);

        services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorageSettings"));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<BlobStorageSettings>>().Value);

        services.AddHttpClient("importPldns", clinet => clinet.Timeout = TimeSpan.FromMinutes(5));
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IJobsRepository, JobsRepository>();
        services.AddScoped<IQualificationsService, QualificationsService>();
        services.AddTransient<IOfqualRegisterService, OfqualRegisterService>();
        services.AddTransient<IOfqualImportService, OfqualImportService>();
        services.AddTransient<IReferenceDataService, ReferenceDataService>();
        services.AddTransient<IFundingEligibilityService, FundingEligibilityService>();
        services.AddScoped<ICsvReaderService, CsvReaderService>();
        services.AddScoped<ISystemClockService, SystemClockService>();
        services.AddScoped<IJobConfigurationService, JobConfigurationService>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<ISchedulerClientService, SchedulerClientService>();
        services.AddScoped<IFundedQualificationWriter, FundedQualificationWriter>();
        services.AddScoped<IQualificationsRepository, QualificationsRepository>();
        services.AddScoped<IImportRepository, ImportRepository>();
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddBlobServiceClient(configuration.GetValue<string>("BlobStorageSettings:ConnectionString"));
        });

        services.AddScoped<IBlobStorageFileService, BlobStorageFileService>();

        var aodpJobsConfiguration = configuration.GetSection(nameof(AodpJobsConfiguration)).Get<AodpJobsConfiguration>();

        services.AddScoped<IOfqualRegisterApi>(provider =>
        {
            const string baseUrl = "https://register-api.ofqual.gov.uk";
            var api = RestClient.For<IOfqualRegisterApi>(baseUrl);
            api.SubscriptionKey = aodpJobsConfiguration.OcpApimSubscriptionKey;
            return api;
        });

        var connectionString = aodpJobsConfiguration.DbConnectionString;

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("DbConnectionString is missing in configuration.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString,
        sqlServerOptions => sqlServerOptions.CommandTimeout(60)));

        services.AddAutoMapper(typeof(MapperProfile));

        return services;
    }
}