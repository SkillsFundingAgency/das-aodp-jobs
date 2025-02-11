using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.AODP.Jobs.StartupExtensions;

var builder = Host.CreateApplicationBuilder();

var configuration = builder.Configuration
    .LoadConfiguration(builder.Services, builder.Environment.IsDevelopment());

builder.Services.AddServiceRegistrations(configuration);
builder.Services.AddLogging();

var app = builder.Build();

app.Run();
