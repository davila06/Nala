namespace PawTrack.Application.Common;

/// <summary>
/// Utilities for masking personally identifiable information (PII) before writing to logs.
/// Never log raw email addresses, phone numbers, or user names.
/// </summary>
public static class PiiHelper
{
    /// <summary>
    /// Returns a masked version of an email address suitable for log output.
    /// Shows the first three characters of the local part and the full domain.
    /// Example: "joh***@example.com"
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "***";

        var at = email.IndexOf('@');
        if (at <= 0)
            return "***";

        var local = email[..at];
        var domain = email[at..]; // includes the @
        var visible = local.Length <= 3 ? local : local[..3];
        return $"{visible}***{domain}";
    }
}
