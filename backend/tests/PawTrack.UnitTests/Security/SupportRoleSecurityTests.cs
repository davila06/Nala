using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Security;

public sealed class SupportRoleSecurityTests
{
    [Fact]
    public void AssignSupportRole_UsesDedicatedRoleWithoutGrantingAdminRole()
    {
        var (user, _) = User.Create("support@example.cr", "hash", "Support Agent");

        user.AssignSupportRole();

        user.Role.Should().Be(UserRole.Support);
        user.Role.Should().NotBe(UserRole.Admin);
    }
}
