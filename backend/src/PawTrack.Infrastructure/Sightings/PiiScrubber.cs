using System.Text.RegularExpressions;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Sightings;

/// <summary>
/// Strips PII (emails, phone numbers, URLs) from free-text notes before persistence.
/// Uses compiled regexes for production-grade performance.
/// </summary>
public sealed partial class PiiScrubber : IPiiScrubber
{
    // RFC-5321 simplified email pattern
    [GeneratedRegex(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    // Costa Rica + international phone numbers (E.164, local formats)
    [GeneratedRegex(
        @"(\+?[0-9]{1,3}[\s\-.]?)?(\(?\d{2,4}\)?[\s\-.]?)?\d{3,4}[\s\-.]?\d{4}",
        RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    // URL pattern — http/https/www
    [GeneratedRegex(
        @"(https?://|www\.)\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    private const string Redacted = "[REDACTED]";

    public string? Scrub(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var result = UrlRegex().Replace(input, Redacted);
        result = EmailRegex().Replace(result, Redacted);
        result = PhoneRegex().Replace(result, Redacted);

        var trimmed = result.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
