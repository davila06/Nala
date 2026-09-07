using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderVerificationRenewalReminderJobTests
{
    [Fact]
    public async Task ExecuteAsync_VerificationExpiringSoon_NotifiesProviderOwner()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var verification = ProviderVerification.Submit(provider.Id, ownerUserId);
        verification.AttachDocument("https://storage.example/document.pdf");
        verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null);
        repository.GetVerificationsExpiringWithinAsync(30, Arg.Any<CancellationToken>()).Returns([verification]);
        repository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        notifications.HasRecentByUserTypeAndEntityAsync(ownerUserId, NotificationType.SystemMessage, verification.Id.ToString(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await new ProviderVerificationRenewalReminderJob(repository, notifications, unitOfWork, NullLogger<ProviderVerificationRenewalReminderJob>.Instance).ExecuteAsync(default);

        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == ownerUserId && notification.RelatedEntityId == verification.Id.ToString()),
            Arg.Any<CancellationToken>());
    }
}