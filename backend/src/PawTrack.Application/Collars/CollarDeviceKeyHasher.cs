using System.Security.Cryptography;

namespace PawTrack.Application.Collars;

public static class CollarDeviceKeyHasher
{
    public static string Compute(string rawKey)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
