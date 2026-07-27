using Microsoft.Azure.Functions.Worker.Http;

namespace SFA.DAS.AODP.Jobs.Functions;

public class SeedQaaQualificationsDataFunction(
    ILogger<SeedQaaQualificationsDataFunction> logger,
    IQaaQualificationSeedService seedService)
{
    [Function("SeedQaaQualificationsDataFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "gov/qaa-qualifications/seed")] HttpRequestData request,
        FunctionContext functionContext)
    {
        var totalRecords = await seedService.SeedAsync(functionContext.CancellationToken);

        return new OkObjectResult($"[SeedQaaQualificationsDataFunction] -> {totalRecords} records seeded.");
    }
}
