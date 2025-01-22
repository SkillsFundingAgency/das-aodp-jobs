using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase;
using SFA.DAS.AODP.Functions.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Services.CSV;


var host = new HostBuilder()

    .ConfigureFunctionsWebApplication()

    .ConfigureServices((context, services) =>
    {
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHttpClient();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IRegulatedQualificationsService, RegulatedQualificationsService>();
        services.AddScoped<ICsvReaderService, CsvReaderService>();
        services.AddScoped<IOfqualRegisterApi>(provider =>
        {
            const string baseUrl = "https://register-api.ofqual.gov.uk";
            var config = provider.GetRequiredService<IConfiguration>();
            var api = RestClient.For<IOfqualRegisterApi>(baseUrl);
            api.SubscriptionKey = config["OcpApimSubscriptionKey"];
            return api;
        });

        services.AddAutoMapper(typeof(MapperProfile));

    })
    .Build();

host.Run();