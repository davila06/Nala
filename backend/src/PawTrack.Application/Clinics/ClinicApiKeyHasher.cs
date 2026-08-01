using System.Security.Cryptography;

namespace PawTrack.Application.Clinics;

public static class ClinicApiKeyHasher
{
    public static string Compute(string rawKey)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
