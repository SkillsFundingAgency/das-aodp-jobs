namespace SFA.DAS.AODP.Jobs;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);

        var configuration = builder.Configuration
            .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

        builder.Services.AddLogging(builder =>
        {
            builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);

#if DEBUG
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
#else
    builder.SetMinimumLevel(LogLevel.Information);
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