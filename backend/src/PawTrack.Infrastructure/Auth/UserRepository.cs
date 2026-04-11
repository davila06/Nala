using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

public sealed class UserRepository(PawTrackDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsTracking()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsTracking()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsTracking()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public void Update(User user) =>
        dbContext.Users.Update(user);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
}
