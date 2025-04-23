## ⛔Never push sensitive information such as client id's, secrets or keys into repositories including in the README file⛔

# AODP Jobs

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_apis/build/status/das-aodp-jobs?branchName=main)](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_build/latest?definitionId=_projectid_&repoName=SkillsFundingAgency%2Fdas-aodp-jobs&branchName=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-aodp-jobs=alert_status)](https://sonarcloud.io/project/overview?id=SkillsFundingAgency_das-aodp-jobs)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)

## About

This repository represents the AODP Jobs code base.  These are services for importing regulated and funded qualifications into the AODP database.
https://skillsfundingagency.atlassian.net.mcas.ms/wiki/spaces/NDL/pages/4895932942/AODP+Azure+Function+Apps

## Requirements 

In order to run this service locally you will need: 
- Install [.NET 8](https://www.microsoft.com/net/download)
- Install [Azure Functions SDK](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)

## Developer Setup

### Environment Setup

* **local.settings.json** - Create a `local.settings.json` file (Copy to Output Directory = Copy always) with the following data:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;",
    "ConfigNames": "SFA.DAS.AODP.Jobs",
    "Version": "1.0",
    "EnvironmentName": "LOCAL"
  }
}
```

* **Azure Table Storage Explorer** - Add the following to your Azure Table Storage Explorer.

    Row Key: SFA.SFA.DAS.AODP.Jobs_1.0

    Partition Key: LOCAL

    Data: [data](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-aodp-jobs/SFA.DAS.AODP.Jobs.json)

### Jobs Configuration
https://skillsfundingagency.atlassian.net.mcas.ms/wiki/spaces/NDL/pages/4895834727/Import+Job+Configuration

### ScheduledImportJobRunner
https://skillsfundingagency.atlassian.net.mcas.ms/wiki/spaces/NDL/pages/4895834703/Scheduled+Import+Job+Runner

### ApprovedQualificationsDataFunction
https://skillsfundingagency.atlassian.net.mcas.ms/wiki/spaces/NDL/pages/4895867415/Funded+Qualifications+Import

### RegulatedQualificationsDataFunction
https://skillsfundingagency.atlassian.net.mcas.ms/wiki/spaces/NDL/pages/4895900111/Regulated+Qualifications+Import 

## Technologies
* .NetCore 8.0
* Azure Functions
* Azure App Insights
* xUnit
* Moq

## License
Licensed under the [MIT license](LICENSE)