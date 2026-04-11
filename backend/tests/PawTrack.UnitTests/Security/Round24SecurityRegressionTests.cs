using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-24 security regression tests.
///
/// Gap: <c>PetsController</c> has <c>[Authorize]</c> at class level but carries
/// <b>zero <c>[EnableRateLimiting]</c> attributes</b> on its four write endpoints:
///   • <c>POST /api/pets</c>
///   • <c>PUT  /api/pets/{id}</c>
///   • <c>DELETE /api/pets/{id}</c>
///   • <c>POST /api/pets/{id}/avatar-token</c>
///
/// The only rate-limited endpoint in the controller is the
/// <c>GET /api/pets/{id}/whatsapp-avatar</c> endpoint, which already has
/// <c>[EnableRateLimiting("whatsapp-avatar")]</c> because it was identified
/// earlier as a heavy image-generation endpoint.
///
/// Attack vector (Azure Blob Storage exhaust — <c>POST</c> and <c>PUT</c>):
///   1. An attacker obtains a valid 15-minute JWT (e.g., via a stolen access token
///      before the round-3 refresh-token theft-detection window closes, or through
///      credential stuffing).
///   2. The attacker sends a tight loop of <c>POST /api/pets</c> requests, each
///      carrying a maximum-size (~5 MB) PNG photo:
///         while true:
///             POST /api/pets  { photo: 5MB_blob, name: "X", ... }
///   3. Because there is no rate limit, the handler:
///        ① Calls <c>IBlobStorageService.UploadAsync</c> on every request — each
///           call writes up to 5 MB to Azure Blob Storage.
///        ② Inserts a new <c>Pet</c> row into the database.
///      At 10 Gbps LAN speed the account can upload ≈ 10 TB / hour to Blob and
///      create millions of DB rows before the access token expires.
///   4. The same attack applies to <c>PUT /api/pets/{id}</c> (which overwrites
///      the Blob on every call).
///
/// Attack vector (DB delete churn — <c>DELETE</c>):
///   1. The attacker creates a large number of pet records (above attack).
///   2. The attacker immediately deletes them in a loop, each call triggering:
///        ① <c>IBlobStorageService.DeleteAsync</c> → Blob Storage API calls
///        ② <c>IPetRepository.Delete</c> + <c>SaveChangesAsync</c> → DB writes
///      Without a rate limit this generates unlimited Blob API call costs.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to:
///     • <c>CreatePet</c>    (<c>POST /api/pets</c>)
///     • <c>UpdatePet</c>    (<c>PUT  /api/pets/{id}</c>)
///     • <c>DeletePet</c>    (<c>DELETE /api/pets/{id}</c>)
///     • <c>GenerateAvatarToken</c> (<c>POST /api/pets/{id}/avatar-token</c>)
/// </summary>
public sealed class Round24SecurityRegressionTests
{
    // ── POST /api/pets — Blob Storage exhaustion ──────────────────────────────

    [Fact]
    public void PetsController_CreatePet_HasEnableRateLimitingAttribute()
    {
        // Each call can upload a 5 MB photo to Azure Blob Storage + insert a DB row.
        // Without a rate limit a compromised 15-minute JWT can push terabytes of
        // data to Blob Storage before the token expires.
        var method = typeof(PetsController)
            .GetMethod("CreatePet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "PetsController must expose a public CreatePet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/pets must carry [EnableRateLimiting] — each call can upload " +
            "up to 5 MB to Azure Blob Storage; without rate limiting a compromised " +
            "JWT can exhaust cloud storage quota before the token expires");
    }

    [Fact]
    public void PetsController_CreatePet_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("CreatePet", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "POST /api/pets must use the 'public-api' policy (30 req/min) — " +
            "consistent with other authenticated write endpoints in the API");
    }

    // ── PUT /api/pets/{id} — Blob overwrite exhaustion ────────────────────────

    [Fact]
    public void PetsController_UpdatePet_HasEnableRateLimitingAttribute()
    {
        // Each call deletes the old Blob entry (if any) and uploads a new 5 MB photo.
        // A tight loop on a single pet ID generates unlimited Blob API churn.
        var method = typeof(PetsController)
            .GetMethod("UpdatePet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "PetsController must expose a public UpdatePet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/pets/{id} must carry [EnableRateLimiting] — repeatedly " +
            "uploading a 5 MB photo to the same pet generates unlimited Blob " +
            "Storage API costs from a single compromised account");
    }

    [Fact]
    public void PetsController_UpdatePet_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("UpdatePet", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }

    // ── DELETE /api/pets/{id} — Blob delete churn ─────────────────────────────

    [Fact]
    public void PetsController_DeletePet_HasEnableRateLimitingAttribute()
    {
        // Each call may invoke IBlobStorageService.DeleteAsync + a DB DELETE.
        // Without a rate limit this generates unbounded Blob API delete calls.
        var method = typeof(PetsController)
            .GetMethod("DeletePet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "PetsController must expose a public DeletePet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "DELETE /api/pets/{id} must carry [EnableRateLimiting] — each call " +
            "can invoke cloud storage deletion + a DB write; without a rate limit " +
            "a compromised account can generate unbounded Blob Storage API call costs");
    }

    [Fact]
    public void PetsController_DeletePet_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("DeletePet", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api");
    }
}
