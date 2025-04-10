# AODP Jobs

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_apis/build/status/das-aodp-jobs?branchName=main)](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_build/latest?definitionId=_projectid_&repoName=SkillsFundingAgency%2Fdas-aodp-jobs&branchName=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-aodp-jobs=alert_status)](https://sonarcloud.io/project/overview?id=SkillsFundingAgency_das-aodp-jobs)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)

This repository represents the AODP Jobs code base.  This is a service...

# Developer Setup
### Requirements

In order to run this service locally you will need: 
- Install [.NET Core 3.1](https://www.microsoft.com/net/download)
- Install [Azure Functions SDK](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)


### Environment Setup

* **local.settings.json** - Create a `local.settings.json` file (Copy to Output Directory = Copy always) with the following data:
```json
{
  "AodpJobsConfiguration": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "OcpApimSubscriptionKey": "19930293b2d94c33b47f5ca5f8f35bd4",
    "DefaultImportPage": "1",
    "DefaultImportLimit": "500",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "EnvironmentName": "LOCAL",
    "ConfigNames": "SFA.DAS.AODP.Jobs",
    "FunctionAppBaseUrl": "http://localhost:7000",
    "FunctionHostKey": ""
  },
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true",
    "Environment": "LOCAL",
    "ConfigNames": "SFA.DAS.AODP.Jobs",
    "Version": "1.0"
  }
}
```

* **Azure Table Storage Explorer** - Add the following to your Azure Table Storage Explorer.

Row Key: SFA.SFA.DAS.AODP.Jobs_1.0

    Partition Key: LOCAL

    Data: [data](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-aodp-jobs/SFA.DAS.AODP.Jobs.json)

    # Sample json
  ```json
  { 
    "AodpJobsConfiguration": { 
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated", 
        "DbConnectionString": "Data Source=DBInstnace;Initial Catalog=SFA.DAS.AODP;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False", 
        "OcpApimSubscriptionKey": "19930293b2d94c33b47f5ca5f8f35bd4", 
        "DefaultImportPage": "1", 
        "DefaultImportLimit": "500", 
        "FundedQualificationsImportUrl": "https://dastestaodpstr2.blob.core.windows.net/funded-qualifications-import/approved.csv?sp=r&st=2025-04-08T11:33:08Z&se=2026-04-08T19:33:08Z&spr=https&sv=2024-11-04&sr=b&sig=J6hXobtYVBVMbhhpx%2BXFSUnVxj9Xz3FdnYBK9zExYFE%3D",
        "ArchivedFundedQualificationsImportUrl":"https://dastestaodpstr2.blob.core.windows.net/funded-qualifications-import/archived.csv?sp=r&st=2025-04-08T11:35:24Z&se=2026-04-08T19:35:24Z&spr=https&sv=2024-11-04&sr=b&sig=%2F7l074oPQ7lo8mMkFNibIqaUOJY%2Bbnui78tL4%2Bh8m6Y%3D",
        "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;", 
        "ConfigNames": "SFA.DAS.AODP.Jobs", 
        "Environment": "LOCAL",
        "FunctionAppBaseUrl": "http://localhost:7000" 
    } 
  }
  ```


### Running

_Details of any APis or other services that are required go here_

### ApprovedQualificationsDataFunction
_Description of what the function does and any configuration needed - or details of how to run and test it succeeded_

### RegulatedQualificationsDataFunction
_Description of what the function does and any configuration needed - or details of how to run and test it succeeded_
