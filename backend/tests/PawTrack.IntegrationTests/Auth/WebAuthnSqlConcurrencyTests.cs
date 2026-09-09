using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Auth;

[Collection("Integration")]
public sealed class WebAuthnSqlConcurrencyTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=PawTrackDev;Integrated Security=True;TrustServerCertificate=True;";

    [Fact]
    public async Task ConcurrentRegistrationOfSameCredentialId_AllowsExactlyOne()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;
        var userId = Guid.CreateVersion7();
        var credentialId = Guid.NewGuid().ToByteArray();

        await using (var setup = new PawTrackDbContext(options))
        {
            var (user, _) = User.Create($"webauthn-{userId:N}@test.invalid", "hash", "WebAuthn test", true);
            setup.Users.Add(user);
            await setup.SaveChangesAsync();
            userId = user.Id;
        }

        try
        {
            async Task<bool> InsertAsync()
            {
                await using var db = new PawTrackDbContext(options);
                db.WebAuthnCredentials.Add(WebAuthnCredential.Create(userId, credentialId, [1, 2, 3], 0));
                try
                {
                    await db.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }

            var results = await Task.WhenAll(InsertAsync(), InsertAsync());
            results.Count(x => x).Should().Be(1);
        }
        finally
        {
            await using var cleanup = new PawTrackDbContext(options);
            var credentials = await cleanup.WebAuthnCredentials.Where(x => x.UserId == userId).ToListAsync();
            cleanup.WebAuthnCredentials.RemoveRange(credentials);
            var user = await cleanup.Users.SingleAsync(x => x.Id == userId);
            cleanup.Users.Remove(user);
            await cleanup.SaveChangesAsync();
        }
    }
}