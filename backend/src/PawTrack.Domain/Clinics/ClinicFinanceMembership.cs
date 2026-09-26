namespace PawTrack.Domain.Clinics;

public enum ClinicFinanceRole { Cashier, Administrator }
public enum ClinicFinancePermission { Collect, Void, CloseCash, Refund, ViewReport, ManageStaff, Fiscal }

public sealed class ClinicFinanceMembership
{
    private ClinicFinanceMembership() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid UserId { get; private set; }
    public ClinicFinanceRole Role { get; private set; }
    public bool IsRevoked { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static ClinicFinanceMembership Grant(Guid clinicId, Guid userId, ClinicFinanceRole role, Guid grantedByUserId)
    {
        if (clinicId == Guid.Empty || userId == Guid.Empty || grantedByUserId == Guid.Empty)
            throw new ArgumentException("Clinic, user and grantor are required.");
        return new ClinicFinanceMembership
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            UserId = userId,
            Role = role,
            GrantedByUserId = grantedByUserId,
            GrantedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool Allows(ClinicFinancePermission permission) => !IsRevoked &&
        (Role == ClinicFinanceRole.Administrator || permission is ClinicFinancePermission.Collect or ClinicFinancePermission.ViewReport);

    public bool CanWorkCrmTask(ClinicCrmTaskType type) => !IsRevoked &&
        (Role == ClinicFinanceRole.Administrator || type == ClinicCrmTaskType.CollectPayment);

    public ClinicInternalTaskRole? InternalTaskRole => IsRevoked ? null : Role switch
    {
        ClinicFinanceRole.Cashier => ClinicInternalTaskRole.Cashier,
        ClinicFinanceRole.Administrator => ClinicInternalTaskRole.Manager,
        _ => null,
    };

    public void ChangeRole(ClinicFinanceRole role) { if (IsRevoked) throw new InvalidOperationException("Membership is revoked."); Role = role; }

    public void Revoke(Guid revokedByUserId)
    {
        if (IsRevoked) throw new InvalidOperationException("Membership is revoked.");
        if (revokedByUserId == Guid.Empty) throw new ArgumentException("Revoker is required.", nameof(revokedByUserId));
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByUserId = revokedByUserId;
    }
}
