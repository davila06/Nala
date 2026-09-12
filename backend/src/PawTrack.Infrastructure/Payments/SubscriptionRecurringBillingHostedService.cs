using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Payments;

/// <summary>
/// Hosted service that checks for active subscriptions expiring within 2 days
/// and automatically processes recurring card charges using stored user payment profiles.
/// Runs once daily at 04:00 Costa Rica time (UTC-6) using PeriodicTimer and IDistributedJobLock.
/// </summary>
public sealed class SubscriptionRecurringBillingHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<SubscriptionRecurringBillingHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);
    private static readonly TimeOnly ScheduledLocalTime = new(4, 0); // 04:00 CR time
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
        logger.LogInformation("SubscriptionRecurringBillingHostedService next run in {Delay}", delay);

        await Task.Delay(delay, stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunCycleAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await using var lease = await jobLock.TryAcquireAsync(
            "SubscriptionRecurringBilling", TimeSpan.FromHours(2), cancellationToken);
        if (lease is null)
            return; // Another instance is already executing this billing cycle

        logger.LogInformation("Starting subscription recurring billing cycle...");

        using var scope = scopeFactory.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserPaymentProfileRepository>();
        var transactionRepo = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var gatewayService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Look for subscriptions due for renewal within the next 2 days
        var expiringSubscriptions = await subscriptionRepo.GetExpiringWithinAsync(2, cancellationToken);
        var renewedCount = 0;
        var failedCount = 0;

        foreach (var sub in expiringSubscriptions)
        {
            if (sub.UserId is null || sub.Status != SubscriptionStatus.Active)
                continue;

            // Check if user has a default saved payment card
            var paymentProfile = await profileRepo.GetDefaultByUserIdAsync(sub.UserId.Value, cancellationToken);
            if (paymentProfile is null)
            {
                // No card on file — user will renew manually via SINPE or web modal
                continue;
            }

            var user = await userRepo.GetByIdAsync(sub.UserId.Value, cancellationToken);
            if (user is null) continue;

            // Compute renewal amount using catalog single-source-of-truth
            var billingMonths = sub.BillingMonths > 0 ? sub.BillingMonths : 1;
            decimal renewalPrice;
            if (SubscriptionPricing.TryGetMonthlyPriceCrc(sub.Tier, out var monthlyPrice))
            {
                renewalPrice = SubscriptionPricing.CalculateTermPriceCrc(monthlyPrice, billingMonths);
            }
            else
            {
                renewalPrice = sub.AmountCrc;
            }

            var orderRef = $"REC-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
            var transaction = PaymentTransaction.Record(
                userId: sub.UserId.Value,
                amountCrc: renewalPrice,
                transactionReference: orderRef,
                purpose: "SubscriptionRecurring",
                paymentProfileId: paymentProfile.Id,
                targetEntityId: sub.Id);

            await transactionRepo.AddAsync(transaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var chargeResult = await gatewayService.ChargeAsync(
                new ChargePaymentRequest(
                    AmountCrc: renewalPrice,
                    PaymentInstrumentOrCustomerToken: paymentProfile.ProviderToken,
                    OrderReference: orderRef,
                    Purpose: "SubscriptionRecurring",
                    CardholderEmail: user.Email),
                cancellationToken);

            if (chargeResult.Success)
            {
                transaction.MarkSucceeded(chargeResult.AuthorizationCode);
                transactionRepo.Update(transaction);

                paymentProfile.RecordUsage();
                profileRepo.Update(paymentProfile);

                sub.RenewRecurring(billingMonths);
                subscriptionRepo.Update(sub);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                renewedCount++;

                try
                {
                    await emailSender.SendRecurringPaymentReceiptAsync(
                        user.Email,
                        user.Name,
                        sub.Tier.ToString(),
                        renewalPrice,
                        paymentProfile.LastFourDigits ?? "4242",
                        sub.ExpiresAt ?? DateTimeOffset.UtcNow.AddMonths(billingMonths),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send recurring receipt email to {Email}", user.Email);
                }
            }
            else
            {
                transaction.MarkFailed(chargeResult.ErrorMessage ?? "Declinado por el banco.");
                transactionRepo.Update(transaction);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                failedCount++;

                var gracePeriod = (sub.ExpiresAt ?? DateTimeOffset.UtcNow).AddDays(3);
                try
                {
                    await emailSender.SendRecurringPaymentFailedAsync(
                        user.Email,
                        user.Name,
                        sub.Tier.ToString(),
                        chargeResult.ErrorMessage ?? "Fondos insuficientes o tarjeta rechazada",
                        gracePeriod,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send payment failure alert to {Email}", user.Email);
                }
            }
        }

        logger.LogInformation(
            "Subscription recurring billing cycle finished. Renewed={Renewed}, Failed={Failed}",
            renewedCount, failedCount);
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = utcNow.ToOffset(CostaRicaOffset);
        var localTodayAt4 = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            ScheduledLocalTime.Hour,
            ScheduledLocalTime.Minute,
            0,
            CostaRicaOffset);

        var nextRun = localNow < localTodayAt4
            ? localTodayAt4
            : localTodayAt4.AddDays(1);

        return nextRun - localNow;
    }
}
