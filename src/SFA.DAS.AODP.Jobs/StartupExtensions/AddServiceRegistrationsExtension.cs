using SFA.DAS.AODP.Jobs.Functions.Abstractions;
using SFA.DAS.AODP.Models.Config;

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

        services.Configure<BlobStorageSettings>(configuration.GetSection("BlobStorageSettings"));
        services.AddSingleton(cfg => cfg.GetRequiredService<IOptions<BlobStorageSettings>>().Value);
        
        services.Configure<StorageConfiguration>(configuration.GetSection(StorageConfiguration.SectionName));

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
        services.AddScoped<IGuidProvider, GuidProvider>();
        services.AddScoped<IJobConfigurationService, JobConfigurationService>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<ISchedulerClientService, SchedulerClientService>();
        services.AddScoped<IFundedQualificationWriter, FundedQualificationWriter>();
        services.AddScoped<IQualificationsRepository, QualificationsRepository>();
        services.AddScoped<IQualificationVersionRepository, QualificationVersionRepository>();
        services.AddScoped<IImportRepository, ImportRepository>();
        services.AddScoped<IQualificationProcessor, QualificationProcessor>();
        services.AddScoped<IQaaSeedCsvBlobReader, QaaSeedCsvBlobReader>();
        services.AddScoped<IQaaQualificationSeedService, QaaQualificationSeedService>();
        services.AddAzureClients(clientBuilder =>
        {
            if (environment.IsDevelopment())
            {
                clientBuilder.AddBlobServiceClient("UseDevelopmentStorage=true").WithName("Local");
            }
            else
            {
                // Need to modify the structure of the settings as its perhaps not entirely correct, but it works for now.
                // Ideally I would make it such that it reads as Storage:Blob:Primary:ServiceUri and Storage:Blob:Secondary:ServiceUri as we have 2 storage accounts currently.
                // So the new structure would suit our infrastructure.

                // Adds in a BlobServiceClient for the two storage accounts into the DI container with a Keyed name to distinguish the two.
                // Use the IAzureClientFactory interface to access a named BlobServiceClient.
                // This approach natively uses ManagedIdentity under the hood by using DefaultAzureCredential.
                clientBuilder.AddBlobServiceClient(new Uri(configuration.GetValue<string>("Storage:ServiceUri")!)).WithName("Storage1");
                clientBuilder.AddBlobServiceClient(new Uri(configuration.GetValue<string>("Storage:ServiceUri2")!)).WithName("Storage2");
            }

            // This is the older approach to an extent where its not using ManagedIdentity but instead using a full connection string
            // Which will either use SAS tokens or the account key, neither is the approach we want to keep.
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

        services.AddTransient<IJobFunctionRunner, JobFunctionRunner>();
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(60));
        });
        
        services.AddDataImportServices(configuration);

        return services;
    }
}