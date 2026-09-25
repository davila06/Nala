using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicStaffAccessRepository(PawTrackDbContext db) : IClinicStaffAccessRepository
{
    public async Task<bool> HasPermissionAsync(Guid clinicId, Guid userId, ClinicStaffPermission permission, CancellationToken cancellationToken = default)
    {
        var member = await db.ClinicStaffMemberships.AsNoTracking()
            .FirstOrDefaultAsync(item => item.ClinicId == clinicId && item.UserId == userId, cancellationToken);
        return member?.Allows(permission) == true;
    }

    public Task<ClinicStaffMembership?> GetAsync(Guid clinicId, Guid userId, CancellationToken cancellationToken = default) =>
        db.ClinicStaffMemberships.FirstOrDefaultAsync(item => item.ClinicId == clinicId && item.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ClinicStaffMemberReadModel>> ListMembersAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        await (from member in db.ClinicStaffMemberships.AsNoTracking()
               join user in db.Users.AsNoTracking() on member.UserId equals user.Id
               where member.ClinicId == clinicId
               orderby member.GrantedAt
               select new ClinicStaffMemberReadModel(member.UserId, user.Email, member.Role, member.VeterinarianId, member.IsRevoked))
            .Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClinicStaffWorkspaceReadModel>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await (from member in db.ClinicStaffMemberships.AsNoTracking()
               join clinic in db.Clinics.AsNoTracking() on member.ClinicId equals clinic.Id
               where member.UserId == userId && !member.IsRevoked && clinic.Status == ClinicStatus.Active
               orderby clinic.Name
               select new ClinicStaffWorkspaceReadModel(clinic.Id, clinic.Name, member.Role))
            .Take(50).ToListAsync(cancellationToken);

    public async Task AddAsync(ClinicStaffMembership member, CancellationToken cancellationToken = default) =>
        await db.ClinicStaffMemberships.AddAsync(member, cancellationToken);
}