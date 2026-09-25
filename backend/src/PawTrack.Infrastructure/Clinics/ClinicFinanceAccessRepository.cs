using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicFinanceAccessRepository(PawTrackDbContext db) : IClinicFinanceAccessRepository
{
    public async Task<IReadOnlyList<ClinicFinanceWorkspaceDto>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await (from member in db.ClinicFinanceMemberships.AsNoTracking()
                                 join clinic in db.Clinics.AsNoTracking() on member.ClinicId equals clinic.Id
                                 where member.UserId == userId && !member.IsRevoked && clinic.Status == ClinicStatus.Active
                                 select new ClinicFinanceWorkspaceDto(clinic.Id, clinic.Name,
                                     member.Role == ClinicFinanceRole.Cashier ? "Cashier" : "Administrator"))
            .Take(50).ToListAsync(cancellationToken);
        var owned = await db.Clinics.AsNoTracking()
            .Where(clinic => clinic.UserId == userId && clinic.Status == ClinicStatus.Active)
            .Select(clinic => new ClinicFinanceWorkspaceDto(clinic.Id, clinic.Name, "Administrator"))
            .Take(50).ToListAsync(cancellationToken);
        return memberships.Concat(owned).OrderBy(workspace => workspace.ClinicName).Take(50).ToList();
    }

    public async Task<bool> HasPermissionAsync(Guid clinicId, Guid userId, ClinicFinancePermission permission, CancellationToken cancellationToken = default)
    {
        var membership = await GetAsync(clinicId, userId, cancellationToken);
        return membership?.Allows(permission) == true;
    }

    public Task<ClinicFinanceMembership?> GetAsync(Guid clinicId, Guid userId, CancellationToken cancellationToken = default) =>
        db.ClinicFinanceMemberships.FirstOrDefaultAsync(x => x.ClinicId == clinicId && x.UserId == userId, cancellationToken);

    public async Task AddAsync(ClinicFinanceMembership membership, CancellationToken cancellationToken = default) =>
        await db.ClinicFinanceMemberships.AddAsync(membership, cancellationToken);

    public async Task<IReadOnlyList<ClinicFinanceMembership>> ListAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await db.ClinicFinanceMemberships.AsNoTracking().Where(x => x.ClinicId == clinicId)
            .OrderBy(x => x.GrantedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicFinanceMemberReadModel>> ListMemberDetailsAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await (from membership in db.ClinicFinanceMemberships.AsNoTracking()
               join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
               where membership.ClinicId == clinicId
               orderby membership.GrantedAt
               select new ClinicFinanceMemberReadModel(membership.Id, membership.UserId, user.Email,
                   membership.Role.ToString(), membership.IsRevoked, membership.GrantedAt))
            .Take(100).ToListAsync(cancellationToken);
}
