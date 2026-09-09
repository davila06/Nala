namespace PawTrack.Application.Certificates.Interfaces;

public interface ICertificateDigitalSigner
{
    Task<CertificateSignature?> SignAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(
        ReadOnlyMemory<byte> content,
        ReadOnlyMemory<byte> signature,
        CancellationToken cancellationToken = default);
}

public sealed record CertificateSignature(byte[] Bytes, string Algorithm);
