using Microsoft.Azure.Functions.Worker.Http;

namespace SFA.DAS.AODP.Jobs.Functions;

public class SeedQaaQualificationsDataFunction(
    ILogger<SeedQaaQualificationsDataFunction> logger,
    QaaSeedDataConfiguration configuration,
    IQaaQualificationSeedService seedService)
{
    [Function("SeedQaaQualificationsDataFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/qaa-qualifications/seed")] HttpRequestData request,
        FunctionContext functionContext)
    {
        if (!configuration.Enabled)
        {
            logger.LogInformation("QAA seed data import skipped because QaaSeedData:Enabled is false.");
            return new BadRequestObjectResult("QAA seed data import is disabled. Set QaaSeedData:Enabled to true to run it.");
        }

        var totalRecords = await seedService.SeedAsync(functionContext.CancellationToken);

        return new OkObjectResult($"[SeedQaaQualificationsDataFunction] -> {totalRecords} records seeded.");
    }
}
