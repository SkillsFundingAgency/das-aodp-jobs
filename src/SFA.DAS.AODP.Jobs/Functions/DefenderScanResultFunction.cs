using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.AODP.Jobs.Functions;

/// <summary>
/// Logs Microsoft Defender for Storage malware scan results delivered by Event Grid.
///
/// This is deliberately log-only. It exists so the infrastructure (Defender -> Event Grid
/// topic -> subscription -> function) can be deployed and proven end to end without waiting
/// on the file metadata tables and repositories. The handling of the results themselves
/// (deleting infected blobs, marking clean ones as safe to access) is being built separately
/// under AWARD-1164 and will replace the body of this function.
/// </summary>
public class DefenderScanResultFunction
{
    private readonly ILogger<DefenderScanResultFunction> _logger;

    public DefenderScanResultFunction(ILogger<DefenderScanResultFunction> logger)
    {
        _logger = logger;
    }

    [Function("DefenderScanResultFunction")]
    public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation(
            "Defender scan result received. EventType: {EventType}, Subject: {Subject}, EventTime: {EventTime}, Id: {Id}",
            eventGridEvent.EventType,
            eventGridEvent.Subject,
            eventGridEvent.EventTime,
            eventGridEvent.Id);

        // Logged in full so the payload shape can be confirmed against a real scan before the
        // handling logic is written against it.
        _logger.LogInformation("Defender scan result payload: {Payload}", eventGridEvent.Data.ToString());
    }
}
