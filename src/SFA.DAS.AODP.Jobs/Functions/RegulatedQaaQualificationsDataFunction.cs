using SFA.DAS.AODP.Jobs.Functions.Abstractions;

namespace SFA.DAS.AODP.Jobs.Functions;

public class RegulatedQaaQualificationsDataFunction(
    ILogger<RegulatedQaaQualificationsDataFunction> logger,
    IQaaQualificationImportService qualificationImportService,
    IJobFunctionRunner jobFunctionRunner)
{
    [Function("RegulatedQaaQualificationsDataFunction")]
    public async Task<IActionResult> Run([TimerTrigger("0 0 2 * * 1")] TimerInfo timerInfo, FunctionContext functionContext)
    {
        return await jobFunctionRunner.RunAsync(
            "RegulatedQaaQualificationsDataFunction", 
            "SYSTEM", 
            JobNames.QaaQualifications, 
            RunImportAsync, 
            functionContext.CancellationToken);
    }

    private async Task<int> RunImportAsync(JobControl control, CancellationToken cancellationToken)
    {
        var jobControl = control as QaaQualificationJobControl;
        var totalRecords = 0;
        if (jobControl!.RunApiImport)
        {
            totalRecords = await qualificationImportService.ImportDataAsync(cancellationToken);
        }

        return totalRecords;
    }
}