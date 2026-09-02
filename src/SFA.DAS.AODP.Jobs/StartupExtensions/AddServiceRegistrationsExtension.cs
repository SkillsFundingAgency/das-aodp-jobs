using Azure.Storage.Blobs;
using SFA.DAS.AODP.Jobs.Functions.Abstractions;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Repositories.Rollover;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Services.Rollover;

namespace SFA.DAS.AODP.Jobs.StartupExtensions;

[ExcludeFromCodeCoverage]
public static class AddServiceRegistrationsExtension
{
    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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
        
        services.Configure<StorageConfiguration>(configuration.GetSection(StorageConfiguration.SectionName));

        services.AddHttpClient("importPldns", clinet => clinet.Timeout = TimeSpan.FromMinutes(5));
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IJobsRepository, JobsRepository>();
        services.AddScoped<IQualificationsService, QualificationsService>();
        services.AddTransient<IOfqualRegisterService, OfqualRegisterService>();
        services.AddTransient<IOfqualImportService, OfqualImportService>();
        services.AddTransient<IReferenceDataService, ReferenceDataService>();
        services.AddTransient<IFundingEligibilityService, FundingEligibilityService>();
        services.AddScoped<IFileProcessingService, FileProcessingService>();
        services.AddScoped<ICsvReaderService, CsvReaderService>();
        services.AddScoped<ISystemClockService, SystemClockService>();
        services.AddScoped<IGuidProvider, GuidProvider>();
        services.AddScoped<IJobConfigurationService, JobConfigurationService>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<ISchedulerClientService, SchedulerClientService>();
        services.AddScoped<IFundedQualificationWriter, FundedQualificationWriter>();
        services.AddScoped<IQualificationsRepository, QualificationsRepository>();
        services.AddScoped<IQualificationVersionRepository, QualificationVersionRepository>();
        services.AddScoped<IImportRepository, ImportRepository>();
        services.AddScoped<IFileRecordRepository, FileRecordRepository>();
        services.AddScoped<IQualificationProcessor, QualificationProcessor>();
        services.AddScoped<IQaaSeedCsvBlobReader, QaaSeedCsvBlobReader>();
        services.AddScoped<IQaaQualificationSeedService, QaaQualificationSeedService>();
        services.AddScoped<IRolloverCandidateRepository, RolloverCandidateRepository>();
        services.AddScoped<IRolloverCandidateService, RolloverCandidateService>();

        services.AddAzureClients(clientBuilder =>
        {
            if (environment.IsDevelopment())
            {
                clientBuilder.AddBlobServiceClient("UseDevelopmentStorage=true").WithName("Local");
            }
            else
            {
                clientBuilder.AddBlobServiceClient(new Uri(configuration.GetValue<string>("Storage:ServiceUri2")!)).WithName("Storage2");
            }
        });

        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();

            if (environment.IsDevelopment())
            {
                // Pin Blob API version so Azurite supports copy/exists operations
                var options = new BlobClientOptions(
                    BlobClientOptions.ServiceVersion.V2023_11_03);

                return new BlobServiceClient("UseDevelopmentStorage=true", options);
            }

            var serviceUri = new Uri(configuration.GetValue<string>("Storage:ServiceUri2")!);
            return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
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

        services.AddTransient<IJobFunctionRunner, JobFunctionRunner>();
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(60));
        });
        
        services.AddDataImportServices(configuration);

        return services;
    }
}
