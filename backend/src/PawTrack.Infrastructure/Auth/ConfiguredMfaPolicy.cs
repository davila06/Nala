using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Auth;

public sealed class ConfiguredMfaPolicy(bool requireForPrivilegedRoles) : IMfaPolicy
{
    public bool RequireForPrivilegedRoles { get; } = requireForPrivilegedRoles;
}