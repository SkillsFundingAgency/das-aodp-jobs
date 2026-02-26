global using AutoMapper;

global using CsvHelper.Configuration;
global using CsvHelper;

global using DocumentFormat.OpenXml.Packaging;
global using DocumentFormat.OpenXml.Spreadsheet;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Azure.Functions.Worker.Builder;
global using Microsoft.Azure.Functions.Worker.Http;
global using Microsoft.Azure.Functions.Worker;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Azure;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging.ApplicationInsights;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using RestEase;

global using SFA.DAS.AODP.Common.Enum;
global using SFA.DAS.AODP.Data.Entities;
global using SFA.DAS.AODP.Data;
global using SFA.DAS.AODP.Infrastructure.Context;
global using SFA.DAS.AODP.Infrastructure.Interfaces;
global using SFA.DAS.AODP.Infrastructure.Repositories;
global using SFA.DAS.AODP.Infrastructure.Services;
global using SFA.DAS.AODP.Jobs.Client;
global using SFA.DAS.AODP.Jobs.Extensions;
global using SFA.DAS.AODP.Jobs.Functions;
global using SFA.DAS.AODP.Jobs.Helpers;
global using SFA.DAS.AODP.Jobs.Interfaces;
global using SFA.DAS.AODP.Jobs.Models;
global using SFA.DAS.AODP.Jobs.Services.CSV;
global using SFA.DAS.AODP.Jobs.Services;
global using SFA.DAS.AODP.Jobs.StartupExtensions;
global using SFA.DAS.AODP.Models.Config;
global using SFA.DAS.AODP.Models.Qualification;
global using SFA.DAS.Configuration.AzureTableStorage;
global using System.Collections.Specialized;
global using System.Diagnostics.CodeAnalysis;
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Text;
