using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFA.DAS.AODP.Models.QaaQualification.Converters;

/// <summary>
/// Defines a converter that allows for converting from a date format found specifically in the Qaa data to
/// a <see cref="DateOnly"/> for the end date field where it comes in as M/YYYY format where the day component is always the last of the month.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class QaaMonthYearToLastDayDateOnlyConverter : JsonConverter<DateOnly>
{
    private static readonly string[] Formats = ["M/yyyy", "MM/yyyy"];

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
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

        var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateOnly(date.Year, date.Month, lastDay);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}