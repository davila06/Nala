namespace PawTrack.Domain.Auth;

public static class UserRoleExtensions
{
    public static bool IsAdminOrSuperAdmin(this UserRole role) =>
        role is UserRole.Admin or UserRole.SuperAdmin;
}
