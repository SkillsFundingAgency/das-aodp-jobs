using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.AODP.Jobs.StartupExtensions;

var builder = FunctionsApplication.CreateBuilder(args);

var configuration = builder.Configuration
    .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddServiceRegistrations(configuration);
builder.Services.AddLogging();

var app = builder.Build();

app.Run();
