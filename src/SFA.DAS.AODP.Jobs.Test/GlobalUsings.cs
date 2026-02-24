global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Azure.Functions.Worker;
global using Microsoft.Extensions.Logging;

global using Moq;

global using RestEase;

global using SFA.DAS.AODP.Common.Enum;
global using SFA.DAS.AODP.Data.Entities;
global using SFA.DAS.AODP.Infrastructure.Repositories;
global using SFA.DAS.AODP.Jobs.Client;
global using SFA.DAS.AODP.Jobs.Functions;
global using SFA.DAS.AODP.Jobs.Interfaces;
global using SFA.DAS.AODP.Jobs.Models.Jobs;
global using SFA.DAS.AODP.Jobs.Services;
global using SFA.DAS.AODP.Models.QaaQualification;
global using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

global using System.Net;