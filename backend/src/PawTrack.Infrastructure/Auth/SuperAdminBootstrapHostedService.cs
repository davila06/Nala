using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Auth;

public sealed class SuperAdminBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IDistributedJobLock distributedJobLock,
    ILogger<SuperAdminBootstrapHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Security:SuperAdmin:BootstrapEnabled", false)) return;
        var email = configuration["Security:SuperAdmin:BootstrapEmail"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogCritical("SuperAdmin bootstrap is enabled but BootstrapEmail is not configured.");
            return;
        }

        await using var lease = await distributedJobLock.TryAcquireAsync(
            "SuperAdminBootstrap", TimeSpan.FromMinutes(5), stoppingToken);
        if (lease is null) return;

        using var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var target = await users.GetByEmailAsync(email, stoppingToken);
        if (target is null)
        {
            logger.LogCritical("SuperAdmin bootstrap target account was not found.");
            return;
        }
        if (target.Role == UserRole.SuperAdmin) return;
        if (!target.IsEmailVerified || !target.HasMfa)
        {
            logger.LogCritical("SuperAdmin bootstrap refused: target must have verified email and MFA configured.");
            return;
        }

        target.AssignSuperAdminRole();
        users.Update(target);
        await audit.AddAsync(AuditLogEntry.Create(
            target.Id, AuditAction.SuperAdminAssigned, "User", target.Id.ToString(),
            "One-time configuration bootstrap"), stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
        logger.LogCritical(
            "SuperAdmin bootstrap completed for UserId={UserId}. Disable Security:SuperAdmin:BootstrapEnabled immediately.",
            target.Id);
    }
}
