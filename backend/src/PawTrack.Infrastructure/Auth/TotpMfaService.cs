using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Auth;

public sealed class TotpMfaService(
    IDataProtectionProvider dataProtectionProvider)
    : IMfaService
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("PawTrack.Mfa.Totp.v1");

    public MfaSetupData CreateSetup(string email)
    {
        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var secret = Base32Encoding.ToString(secretBytes);
        var issuer = "PawTrack CR";
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        return new MfaSetupData(secret, uri);
    }

    public string ProtectSecret(string secret) => protector.Protect(secret);

    public bool Verify(string protectedSecret, string code)
    {
        try
        {
            var secret = protector.Unprotect(protectedSecret);
            var totp = new Totp(Base32Encoding.ToBytes(secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch (Exception) when (protectedSecret.Length > 0)
        {
            return false;
        }
    }
}