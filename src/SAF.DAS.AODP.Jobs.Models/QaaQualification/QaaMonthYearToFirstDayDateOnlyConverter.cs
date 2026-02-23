using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFA.DAS.AODP.Models.QaaQualification;

/// <summary>
/// Defines a converter that allows for converting from a date format found specifically in the Qaa data to
/// a <see cref="DateOnly"/> for the start date field where it comes in as M/YYYY format where the day component is always the first of the month.
/// </summary>
public sealed class QaaMonthYearToFirstDayDateOnlyConverter : JsonConverter<DateOnly>
{
    private static readonly string[] Formats = ["M/yyyy", "MM/yyyy"];

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Expect a non-null string like "9/2024" or "09/2024"
        var value = reader.GetString()?.Replace(" ", "");

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Date value was null or empty. Expecting M/yyyy or MM/yyyy.");
        }

        if (!DateOnly.TryParseExact(
                value.Trim(),
                Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw new JsonException($"Invalid month/year format: '{value}'. Expected 'M/yyyy' or 'MM/yyyy'.");
        }

        return new DateOnly(date.Year, date.Month, 1);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}