using System.Globalization;

namespace SFA.DAS.AODP.Jobs;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        // 1. PLACE IT HERE: Define and enforce the global culture first thing
        var targetCulture = new CultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentCulture = targetCulture;
        CultureInfo.DefaultThreadCurrentUICulture = targetCulture;

        var builder = FunctionsApplication.CreateBuilder(args);

        var configuration = builder.Configuration
            .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

        builder.Services.AddLogging(builder =>
        {
            builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
            builder.SetMinimumLevel(LogLevel.Information);

#if DEBUG
            builder.AddConsole();
#endif
        });

        builder.Services
            .AddApplicationInsightsTelemetryWorkerService()    
            .ConfigureFunctionsApplicationInsights();
        builder.Services.AddServiceRegistrations(configuration);

        var app = builder.Build();
        
        app.Run();
    }
}