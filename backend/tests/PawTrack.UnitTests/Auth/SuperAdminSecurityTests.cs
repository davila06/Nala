using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PawTrack.Application.Auth.Commands.AssignSuperAdminRole;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Auth;

namespace PawTrack.UnitTests.Auth;

public sealed class SuperAdminSecurityTests
{
    [Fact]
    public void GenerateAccessToken_ForSuperAdmin_EmitsPrimaryAndInheritedAdminClaims()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-signing-key-at-least-32-bytes-long",
            ["Jwt:Issuer"] = "pawtrack-tests",
            ["Jwt:Audience"] = "pawtrack-tests",
        }).Build();
        var service = new JwtTokenService(configuration);

        var token = service.GenerateAccessToken(Guid.NewGuid(), "root@pawtrack.cr", "Root", UserRole.SuperAdmin);
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();

        claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value)
            .Should().BeEquivalentTo(["SuperAdmin", "Admin"]);
        claims.Should().Contain(x => x.Type == "platform_role" && x.Value == "SuperAdmin");
        claims.Should().Contain(x => x.Type == "mfa" && x.Value == "true");
    }

    [Fact]
    public async Task AssignSuperAdmin_WhenActorIsAdmin_ReturnsFailureWithoutMutation()
    {
        var users = Substitute.For<IUserRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var mfa = Substitute.For<IMfaService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (actor, _) = User.Create("admin@pawtrack.cr", "hash", "Admin");
        actor.PromoteToAdmin();
        var (target, _) = User.Create("target@pawtrack.cr", "hash", "Target");
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = new AssignSuperAdminRoleCommandHandler(users, audit, mfa, unitOfWork);
        var result = await handler.Handle(new AssignSuperAdminRoleCommand(actor.Id, target.Id, "approved change", "123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        target.Role.Should().Be(UserRole.Owner);
        await audit.DidNotReceive().AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignSuperAdmin_WhenActorIsSuperAdminAndTargetHasMfa_ElevatesAndAudits()
    {
        var users = Substitute.For<IUserRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var mfa = Substitute.For<IMfaService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (actor, _) = User.Create("root@pawtrack.cr", "hash", "Root");
        actor.ConfigureMfa("protected-actor-secret");
        actor.AssignSuperAdminRole();
        var (target, verificationToken) = User.Create("target@pawtrack.cr", "hash", "Target");
        target.VerifyEmail(verificationToken);
        target.ConfigureMfa("protected-secret");
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        mfa.Verify(actor.MfaSecretProtected!, "123456").Returns(true);

        var handler = new AssignSuperAdminRoleCommandHandler(users, audit, mfa, unitOfWork);
        var result = await handler.Handle(new AssignSuperAdminRoleCommand(actor.Id, target.Id, "security council approval", "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.SuperAdmin);
        await audit.Received(1).AddAsync(Arg.Is<AuditLogEntry>(x =>
            x.Action == AuditAction.SuperAdminAssigned && x.AdminUserId == actor.Id), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignSuperAdmin_WhenFreshMfaIsInvalid_ReturnsFailure()
    {
        var users = Substitute.For<IUserRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var mfa = Substitute.For<IMfaService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (actor, _) = User.Create("root@pawtrack.cr", "hash", "Root");
        actor.ConfigureMfa("protected-secret");
        actor.AssignSuperAdminRole();
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        mfa.Verify(actor.MfaSecretProtected!, "000000").Returns(false);

        var handler = new AssignSuperAdminRoleCommandHandler(users, audit, mfa, unitOfWork);
        var result = await handler.Handle(
            new AssignSuperAdminRoleCommand(actor.Id, Guid.NewGuid(), "security approval", "000000"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Fresh MFA verification is required.");
    }

    [Fact]
    public async Task RevokeSuperAdmin_WhenTargetIsLastSuperAdmin_ReturnsFailure()
    {
        var users = Substitute.For<IUserRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var mfa = Substitute.For<IMfaService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (actor, _) = User.Create("root@pawtrack.cr", "hash", "Root");
        actor.ConfigureMfa("actor-secret"); actor.AssignSuperAdminRole();
        var (target, _) = User.Create("target@pawtrack.cr", "hash", "Target");
        target.ConfigureMfa("target-secret"); target.AssignSuperAdminRole();
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        users.CountByRoleAsync(UserRole.SuperAdmin, Arg.Any<CancellationToken>()).Returns(1);
        mfa.Verify(actor.MfaSecretProtected!, "123456").Returns(true);

        var handler = new RevokeSuperAdminRoleCommandHandler(users, audit, mfa, unitOfWork);
        var result = await handler.Handle(
            new RevokeSuperAdminRoleCommand(actor.Id, target.Id, "access removal", "123456"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("The last SuperAdmin cannot be revoked.");
        target.Role.Should().Be(UserRole.SuperAdmin);
    }
}
