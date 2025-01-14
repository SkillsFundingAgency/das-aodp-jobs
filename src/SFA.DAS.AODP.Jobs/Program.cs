using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.StartupExtensions;

var host = new HostBuilder()

    .ConfigureFunctionsWebApplication()

    .ConfigureServices((context, services) =>
    {
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
    })
    .Build();

host.Run();