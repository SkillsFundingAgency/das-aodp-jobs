using System.Text.Json.Serialization;

namespace SFA.DAS.AODP.Models.Qualification;

public class PaginatedResult<T>
{
    [JsonPropertyName("results")]
    public required List<T>? Results { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}