var builder = FunctionsApplication.CreateBuilder(args);

var configuration = builder.Configuration
    .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

builder.Services.AddLogging(builder =>
{
    builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
    builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
    builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    builder.AddFilter(typeof(SFA.DAS.AODP.Jobs.Program).Namespace, LogLevel.Information);

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

#pragma warning disable S1118
// Bit of a workaround for now, as we use top level statements for the Program.cs and the compiler automatically generates a Program class under the hood, we need a way to assign them [ExcludeFromCodeCoverage] attribute so having a partial class solves this
namespace SFA.DAS.AODP.Jobs
{
    [ExcludeFromCodeCoverage]
    public partial class Program
    {
    }
}
#pragma warning restore S1118