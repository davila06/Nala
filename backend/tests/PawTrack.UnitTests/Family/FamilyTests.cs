using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Family;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Family;
using PawTrack.Domain.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace PawTrack.UnitTests.Family;

// ── FamilyInvitation domain ───────────────────────────────────────────────────

public sealed class FamilyInvitationDomainTests
{
    [Fact]
    public void Create_GeneratesNonEmptyToken()
    {
        var inv = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        inv.Token.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TwoInvitations_HaveDifferentTokens()
    {
        var id = Guid.NewGuid();
        var a = FamilyInvitation.Create(id, "a@test.com");
        var b = FamilyInvitation.Create(id, "a@test.com");
        a.Token.Should().NotBe(b.Token);
    }

    [Fact]
    public void IsExpired_FreshInvitation_IsFalse()
    {
        var inv = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        inv.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsAccepted_BeforeAccept_IsFalse()
    {
        var inv = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        inv.IsAccepted.Should().BeFalse();
    }

    [Fact]
    public void Accept_SetsAcceptedAt()
    {
        var inv = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        inv.Accept();
        inv.IsAccepted.Should().BeTrue();
        inv.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void InvitedEmail_IsLowercased()
    {
        var inv = FamilyInvitation.Create(Guid.NewGuid(), "BOB@TEST.COM");
        inv.InvitedEmail.Should().Be("bob@test.com");
    }
}

// ── AcceptFamilyInvitationCommandHandler ─────────────────────────────────────

public sealed class AcceptFamilyInvitationTests
{
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly IDistributedJobLock _jobLock = Substitute.For<IDistributedJobLock>();
    private readonly AcceptFamilyInvitationCommandHandler _sut;

    public AcceptFamilyInvitationTests()
    {
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _entitlements.AuthorizeAsync(
                Arg.Any<Guid>(), "MaxFamilyMembers", 1m, Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(true, true, 5m, 0m, 5m, null, SubscriptionTier.UserFamilia));
        _jobLock.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Substitute.For<IAsyncDisposable>());
        _sut = new AcceptFamilyInvitationCommandHandler(_familyRepo, _userRepo, _uow, _entitlements, _jobLock);
    }

    [Fact]
    public async Task Handle_AtMemberLimit_RejectsInvitationWithoutAddingMembership()
    {
        var ownerId = Guid.NewGuid();
        var account = FamilyAccount.Create(ownerId, "Test Family");
        var invitation = FamilyInvitation.Create(account.Id, "new-member@test.com");
        var (user, verificationToken) = User.Create("new-member@test.com", "hash", "New Member");
        user.VerifyEmail(verificationToken);

        _familyRepo.GetInvitationByTokenAsync(invitation.Token, Arg.Any<CancellationToken>()).Returns(invitation);
        _familyRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _familyRepo.CountActiveMembersAsync(account.Id, Arg.Any<CancellationToken>()).Returns(5);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(user.Id, invitation.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        invitation.IsAccepted.Should().BeFalse();
        await _familyRepo.DidNotReceive().AddMembershipAsync(
            Arg.Any<FamilyMembership>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFamilyCapacityLockIsUnavailable_DoesNotAcceptInvitation()
    {
        var ownerId = Guid.NewGuid();
        var account = FamilyAccount.Create(ownerId, "Test Family");
        var invitation = FamilyInvitation.Create(account.Id, "new-member@test.com");
        _familyRepo.GetInvitationByTokenAsync(invitation.Token, Arg.Any<CancellationToken>()).Returns(invitation);
        _familyRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _jobLock.TryAcquireAsync($"FamilyCapacity:{account.Id:N}", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((IAsyncDisposable?)null);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(Guid.NewGuid(), invitation.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        invitation.IsAccepted.Should().BeFalse();
        await _familyRepo.DidNotReceive().AddMembershipAsync(
            Arg.Any<FamilyMembership>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CorrectEmailAndValidToken_AcceptsAndCreatesMembership()
    {
        var account = FamilyAccount.Create(Guid.NewGuid(), "Test Family");
        var invitation = FamilyInvitation.Create(account.Id, "bob@test.com");
        var (user, token) = User.Create("bob@test.com", "hash", "Bob");
        user.VerifyEmail(token);

        _familyRepo.GetInvitationByTokenAsync(invitation.Token, Arg.Any<CancellationToken>())
                   .Returns(invitation);
        _familyRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _familyRepo.CountActiveMembersAsync(account.Id, Arg.Any<CancellationToken>()).Returns(1);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(user.Id, invitation.Token), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invitation.IsAccepted.Should().BeTrue();
        await _familyRepo.Received(1).AddMembershipAsync(
            Arg.Is<FamilyMembership>(m => m.UserId == user.Id && m.Role == FamilyMemberRole.Member),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongEmail_ReturnsFailure()
    {
        var invitation = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        var (user, token) = User.Create("alice@test.com", "hash", "Alice");
        user.VerifyEmail(token);

        _familyRepo.GetInvitationByTokenAsync(invitation.Token, Arg.Any<CancellationToken>())
                   .Returns(invitation);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(user.Id, invitation.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("correo"));
        await _familyRepo.DidNotReceive().AddMembershipAsync(Arg.Any<FamilyMembership>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedInvitation_ReturnsFailure()
    {
        var invitation = FamilyInvitation.Create(Guid.NewGuid(), "bob@test.com");
        invitation.Accept();

        _familyRepo.GetInvitationByTokenAsync(invitation.Token, Arg.Any<CancellationToken>())
                   .Returns(invitation);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(Guid.NewGuid(), invitation.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsFailure()
    {
        _familyRepo.GetInvitationByTokenAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                   .Returns((FamilyInvitation?)null);

        var result = await _sut.Handle(
            new AcceptFamilyInvitationCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

public sealed class GetFamilyMembersQueryTests
{
    [Fact]
    public async Task Handle_ReturnsPendingInvitationCountAlongsideActiveMembers()
    {
        var ownerId = Guid.NewGuid();
        var account = FamilyAccount.Create(ownerId, "Test Family");
        var ownerMembership = FamilyMembership.CreateOwner(account.Id, ownerId);
        var (owner, verificationToken) = User.Create("owner@test.com", "hash", "Owner");
        owner.VerifyEmail(verificationToken);
        var familyRepository = Substitute.For<IFamilyRepository>();
        familyRepository.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>()).Returns(account);
        familyRepository.GetActiveMembershipsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FamilyMembership>)[ownerMembership]);
        familyRepository.CountPendingInvitationsAsync(account.Id, Arg.Any<CancellationToken>()).Returns(2);
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<User>)[owner]);
        var handler = new GetFamilyMembersQueryHandler(familyRepository, userRepository);

        var result = await handler.Handle(new GetFamilyMembersQuery(ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PendingInvitations.Should().Be(2);
        result.Value.PendingInvitationLimit.Should().Be(3);
    }
}

// ── InviteFamilyMemberCommandHandler: max pending ─────────────────────────────

public sealed class InviteMemberPendingLimitTests
{
    private readonly IFamilyRepository _repo = Substitute.For<IFamilyRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly IDistributedJobLock _jobLock = Substitute.For<IDistributedJobLock>();

    public InviteMemberPendingLimitTests()
    {
        _entitlements.AuthorizeAsync(
                Arg.Any<Guid>(), "MaxFamilyMembers", 1m, Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(true, true, 5m, 0m, 5m, null, SubscriptionTier.UserFamilia));
        _jobLock.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Substitute.For<IAsyncDisposable>());
    }

    private FamilyAccount MakeAccount(Guid ownerId)
    {
        var account = FamilyAccount.Create(ownerId, "Test Family");
        typeof(FamilyAccount).GetProperty("Id")!.SetValue(account, Guid.NewGuid());
        typeof(FamilyAccount).GetProperty("OwnerId")!.SetValue(account, ownerId);
        return account;
    }

    [Fact]
    public async Task Handle_BelowPendingLimit_CreatesInvitation()
    {
        var ownerId = Guid.NewGuid();
        var account = MakeAccount(ownerId);
        _repo.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>()).Returns(account);
        _repo.CountActiveMembersAsync(account.Id, Arg.Any<CancellationToken>()).Returns(1);
        _repo.CountPendingInvitationsAsync(account.Id, Arg.Any<CancellationToken>()).Returns(2); // below 3
        _email.SendFamilyInvitationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(Task.CompletedTask);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = new InviteFamilyMemberCommandHandler(_repo, _email, _uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteFamilyMemberCommandHandler>.Instance, _entitlements, _jobLock);
        var result = await sut.Handle(
            new InviteFamilyMemberCommand(ownerId, "bob@test.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AtPendingLimit_ReturnsFailure()
    {
        var ownerId = Guid.NewGuid();
        var account = MakeAccount(ownerId);
        _repo.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>()).Returns(account);
        _repo.CountActiveMembersAsync(account.Id, Arg.Any<CancellationToken>()).Returns(1);
        _repo.CountPendingInvitationsAsync(account.Id, Arg.Any<CancellationToken>()).Returns(3); // at limit

        var sut = new InviteFamilyMemberCommandHandler(_repo, _email, _uow,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InviteFamilyMemberCommandHandler>.Instance, _entitlements, _jobLock);
        var result = await sut.Handle(
            new InviteFamilyMemberCommand(ownerId, "charlie@test.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("pendientes"));
        await _repo.DidNotReceive().AddInvitationAsync(Arg.Any<FamilyInvitation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PendingInvitationsReserveRemainingFamilySeats()
    {
        var ownerId = Guid.NewGuid();
        var account = MakeAccount(ownerId);
        _repo.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>()).Returns(account);
        _repo.CountActiveMembersAsync(account.Id, Arg.Any<CancellationToken>()).Returns(4);
        _repo.CountPendingInvitationsAsync(account.Id, Arg.Any<CancellationToken>()).Returns(1);

        var sut = new InviteFamilyMemberCommandHandler(_repo, _email, _uow,
            NullLogger<InviteFamilyMemberCommandHandler>.Instance, _entitlements, _jobLock);
        var result = await sut.Handle(
            new InviteFamilyMemberCommand(ownerId, "fifth-member@test.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddInvitationAsync(Arg.Any<FamilyInvitation>(), Arg.Any<CancellationToken>());
    }
}
