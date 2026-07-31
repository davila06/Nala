using System.Security.Cryptography;
using PawTrack.Application.Subscriptions.Interfaces;

namespace PawTrack.Infrastructure.Subscriptions;

/// <summary>Generates SINPE Móvil payment references using a CSPRNG (no external gateway in MVP).</summary>
public sealed class SinpePaymentService : IPaymentService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // remove ambiguous chars

    public string GenerateReference()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(8, bytes.ToArray(), static (span, b) =>
        {
            for (int i = 0; i < 8; i++)
                span[i] = Alphabet[b[i] % Alphabet.Length];
        });
    }
}
