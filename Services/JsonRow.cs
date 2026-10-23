using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LaboratoryManagementSystem.Web.Services;

/// <summary>Small helpers for reading rows returned by the LMS API (case-insensitive property names).</summary>
public static class JsonRow
{
    public static JsonElement? Find(JsonElement row, string name)
    {
        if (row.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in row.EnumerateObject())
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        return null;
    }

    public static string Str(JsonElement row, string name, string fallback = "—")
    {
        var found = Find(row, name);
        if (found is null) return fallback;
        var value = found.Value;
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => fallback,
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? fallback : value.GetString()!,
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            _ => value.ToString()
        };
    }

    public static int Int(JsonElement row, string name)
    {
        var found = Find(row, name);
        if (found is { } value && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        return 0;
    }

    public static bool Bool(JsonElement row, string name)
    {
        var found = Find(row, name);
        return found is { } value && value.ValueKind == JsonValueKind.True;
    }

    public static DateTime? Date(JsonElement row, string name)
    {
        var found = Find(row, name);
        if (found is { } value && value.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return null;
    }

    public static string When(DateTime? date) => date.HasValue ? date.Value.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) : "—";

    public static string Time(DateTime? date) => date.HasValue ? date.Value.ToString("h:mm tt", CultureInfo.InvariantCulture) : "—";

    public static string Day(DateTime? date) => date.HasValue ? date.Value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) : "—";

    public static string Csv(IEnumerable<string> header, IEnumerable<IEnumerable<string>> rows)
    {
        static string Cell(string value)
        {
            var text = value ?? "";
            if (text.Length > 0 && "=+-@".Contains(text[0]) && text != "—") text = "'" + text; // avoid spreadsheet formulas
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", header.Select(Cell)));
        foreach (var row in rows) builder.AppendLine(string.Join(",", row.Select(Cell)));
        return builder.ToString();
    }
}
