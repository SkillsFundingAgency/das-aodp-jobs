using System.Text.Json.Serialization;

namespace SFA.DAS.AODP.Jobs.Models.Jobs
{
    public sealed class DefenderScanEvent
    {
        [JsonPropertyName("blobUri")]
        public string? BlobUri { get; set; }

        [JsonPropertyName("eTag")]
        public string? ETag { get; set; }

        [JsonPropertyName("scanResultType")]
        public string? ScanResultType { get; set; }
    }
}
