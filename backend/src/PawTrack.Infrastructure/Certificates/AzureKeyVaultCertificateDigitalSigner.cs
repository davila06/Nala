using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Certificates.Interfaces;
using System.Security.Cryptography;

namespace PawTrack.Infrastructure.Certificates;

/// <summary>Signs certificate bytes with a Key Vault key using Managed Identity.</summary>
public sealed class AzureKeyVaultCertificateDigitalSigner(IConfiguration configuration) : ICertificateDigitalSigner
{
    public async Task<CertificateSignature?> SignAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var keyId = configuration["Certificates:KeyVaultKeyId"];
        if (string.IsNullOrWhiteSpace(keyId)) return null;
        var client = new CryptographyClient(new Uri(keyId), new DefaultAzureCredential());
        var result = await client.SignDataAsync(SignatureAlgorithm.RS256, content.ToArray(), cancellationToken);
        return new CertificateSignature(result.Signature, "RSA-SHA256-PKCS1-KeyVault");
    }

    public async Task<bool> VerifyAsync(
        ReadOnlyMemory<byte> content,
        ReadOnlyMemory<byte> signature,
        CancellationToken cancellationToken = default)
    {
        var keyId = configuration["Certificates:KeyVaultKeyId"];
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            var client = new CryptographyClient(new Uri(keyId), new DefaultAzureCredential());
            var result = await client.VerifyDataAsync(
                SignatureAlgorithm.RS256, content.ToArray(), signature.ToArray(), cancellationToken);
            return result.IsValid;
        }

        var publicKey = configuration["Certificates:SigningPublicKeyBase64"];
        if (string.IsNullOrWhiteSpace(publicKey)) return false;
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        return rsa.VerifyData(content.Span, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
