using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ReviewServiceProviderNotificationTests
{
    [Fact]
    public async Task Handle_ApprovedProvider_NotifiesOwner()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming CR", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ReviewServiceProviderCommandHandler(providers, audit, unitOfWork, notifications);
        var result = await handler.Handle(new ReviewServiceProviderCommand(Guid.NewGuid(), provider.Id, true), default);

        result.IsSuccess.Should().BeTrue();
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == provider.UserId && notification.Type == NotificationType.ProviderStatusUpdate),
            Arg.Any<CancellationToken>());
    }
}