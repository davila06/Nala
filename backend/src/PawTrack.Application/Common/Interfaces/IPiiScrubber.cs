namespace PawTrack.Application.Common.Interfaces;

/// <summary>Strips Personally Identifiable Information from free-text notes before persistence.</summary>
public interface IPiiScrubber
{
    /// <summary>
    /// Returns the sanitised version of <paramref name="input"/>.
    /// Removes email addresses, phone numbers, and URLs from the text.
    /// Returns <see langword="null"/> if the input is null or whitespace.
    /// </summary>
    string? Scrub(string? input);
}
