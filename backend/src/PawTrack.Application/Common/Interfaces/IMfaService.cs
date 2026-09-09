namespace PawTrack.Application.Common.Interfaces;

public interface IMfaService
{
    MfaSetupData CreateSetup(string email);
    string ProtectSecret(string secret);
    bool Verify(string protectedSecret, string code);
}

public sealed record MfaSetupData(string Secret, string OtpAuthUri);