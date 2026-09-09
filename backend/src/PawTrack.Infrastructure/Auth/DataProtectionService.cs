using Microsoft.AspNetCore.DataProtection;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Auth;

public sealed class DataProtectionService(IDataProtectionProvider provider) : IDataProtectionService
{
    private readonly IDataProtector protector = provider.CreateProtector("PawTrack.Webhooks.Secrets.v1");
    public string Protect(string value) => protector.Protect(value);
    public string Unprotect(string value) => protector.Unprotect(value);
}