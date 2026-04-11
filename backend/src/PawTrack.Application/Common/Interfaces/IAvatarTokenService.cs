namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Service to generate and validate HMAC-signed ephemeral tokens for the WhatsApp avatar endpoint.
/// Tokens are stateless (no DB required) and expire after a configurable window.
/// </summary>
public interface IAvatarTokenService
{
    /// <summary>Generates a signed token valid for <c>ExpiryMinutes</c> for the given pet.</summary>
    string Generate(Guid petId);

    /// <summary>Returns true if <paramref name="token"/> was issued for <paramref name="petId"/> and has not expired.</summary>
    bool Validate(Guid petId, string token);
}
