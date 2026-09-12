using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Payments;

public sealed class UserPaymentProfileRepository(PawTrackDbContext db) : IUserPaymentProfileRepository
{
    public async Task<UserPaymentProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.UserPaymentProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserPaymentProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.UserPaymentProfiles
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<UserPaymentProfile?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.UserPaymentProfiles
            .Where(x => x.UserId == userId && x.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(UserPaymentProfile profile, CancellationToken cancellationToken = default) =>
        await db.UserPaymentProfiles.AddAsync(profile, cancellationToken);

    public void Update(UserPaymentProfile profile) => db.UserPaymentProfiles.Update(profile);

    public void Delete(UserPaymentProfile profile) => db.UserPaymentProfiles.Remove(profile);
}
