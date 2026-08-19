using System.Security.Cryptography;

namespace PawTrack.Domain.Family;

public enum FamilyMemberRole { Owner, Member }

public sealed class FamilyAccount
{
    private FamilyAccount() { }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static FamilyAccount Create(Guid ownerId, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        OwnerId = ownerId,
        Name = name.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };
}

public sealed class FamilyMembership
{
    private FamilyMembership() { }

    public Guid Id { get; private set; }
    public Guid FamilyAccountId { get; private set; }
    public Guid UserId { get; private set; }
    public FamilyMemberRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static FamilyMembership CreateOwner(Guid familyAccountId, Guid ownerId) => new()
    {
        Id = Guid.CreateVersion7(),
        FamilyAccountId = familyAccountId,
        UserId = ownerId,
        Role = FamilyMemberRole.Owner,
        JoinedAt = DateTimeOffset.UtcNow,
        IsActive = true,
    };

    public static FamilyMembership CreateMember(Guid familyAccountId, Guid userId) => new()
    {
        Id = Guid.CreateVersion7(),
        FamilyAccountId = familyAccountId,
        UserId = userId,
        Role = FamilyMemberRole.Member,
        JoinedAt = DateTimeOffset.UtcNow,
        IsActive = true,
    };

    public void Deactivate() => IsActive = false;
}

public sealed class FamilyInvitation
{
    private FamilyInvitation() { }

    public Guid Id { get; private set; }
    public Guid FamilyAccountId { get; private set; }
    public string InvitedEmail { get; private set; } = string.Empty;
    public Guid Token { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsAccepted => AcceptedAt.HasValue;

    public static FamilyInvitation Create(Guid familyAccountId, string invitedEmail) => new()
    {
        Id = Guid.CreateVersion7(),
        FamilyAccountId = familyAccountId,
        InvitedEmail = invitedEmail.Trim().ToLowerInvariant(),
        Token = new Guid(RandomNumberGenerator.GetBytes(16)),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public void Accept()
    {
        AcceptedAt = DateTimeOffset.UtcNow;
    }
}
