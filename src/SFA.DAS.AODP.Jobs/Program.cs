using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using SFA.DAS.AODP.Jobs.StartupExtensions;

var builder = FunctionsApplication.CreateBuilder(args);

var configuration = builder.Configuration
    .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

builder.Services.AddLogging(builder =>
{
    builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
    builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);

    builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();

});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()    
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddServiceRegistrations(configuration);

var app = builder.Build();

app.Run();
