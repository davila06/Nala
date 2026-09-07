using System.Text;

namespace PawTrack.Application.Regulatory.Renderers;

public sealed record ReportRow(string Canton, string Dimension, string Value);

public static class CsvReportRenderer
{
    public static string Render(IEnumerable<ReportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Canton,Dimension,Value");
        foreach (var row in rows)
        {
            builder.Append(Escape(row.Canton)).Append(',')
                .Append(Escape(row.Dimension)).Append(',')
                .Append(Escape(row.Value)).AppendLine();
        }

        return builder.ToString();
    }

    public static byte[] RenderUtf8(IEnumerable<ReportRow> rows) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Render(rows))).ToArray();

    private static string Escape(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@')
            safe = "'" + safe;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
