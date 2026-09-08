using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.AssignSupportRole;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Security;

public sealed class AssignSupportRoleCommandTests
{
    [Fact]
    public async Task Handle_AssignsSupportRoleToNonAdminUser()
    {
        var users = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (target, _) = User.Create("support@example.cr", "hash", "Support");
        users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = new AssignSupportRoleCommandHandler(users, unitOfWork);
        var result = await handler.Handle(new AssignSupportRoleCommand(Guid.NewGuid(), target.Id), default);

        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.Support);
        users.Received().Update(target);
    }

    [Fact]
    public async Task Handle_RejectsAdminTarget()
    {
        var users = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var (target, _) = User.Create("admin@example.cr", "hash", "Admin");
        target.PromoteToAdmin();
        users.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = new AssignSupportRoleCommandHandler(users, unitOfWork);
        var result = await handler.Handle(new AssignSupportRoleCommand(Guid.NewGuid(), target.Id), default);

        result.IsFailure.Should().BeTrue();
        target.Role.Should().Be(UserRole.Admin);
    }
}
