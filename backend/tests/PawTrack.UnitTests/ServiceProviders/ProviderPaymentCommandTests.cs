using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Application.ServiceProviders.Payments;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderPaymentCommandTests
{
    [Fact]
    public async Task CreatePayment_ReturnsExistingPaymentForRepeatedIdempotencyKey()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var paymentService = Substitute.For<IPaymentService>();
        var paymentGateway = Substitute.For<IProviderPaymentGateway>();
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        var payment = ProviderPayment.Create(
            booking.Id, booking.CustomerUserId, booking.ServiceProviderId, 25_000m, "SINPE-001", "idem-001");
        repository.GetPaymentByIdempotencyKeyAsync("idem-001", Arg.Any<CancellationToken>()).Returns(payment);

        var handler = new CreateProviderBookingPaymentCommandHandler(repository, unitOfWork, paymentService, paymentGateway);
        var result = await handler.Handle(
            new CreateProviderBookingPaymentCommand(booking.CustomerUserId, booking.Id, "idem-001"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(payment.Id);
        await repository.DidNotReceive().AddPaymentAsync(Arg.Any<ProviderPayment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmPayment_ConfirmsPaymentAndBookingForAdmin()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        booking.MarkAwaitingPayment();
        var payment = ProviderPayment.Create(
            booking.Id, booking.CustomerUserId, booking.ServiceProviderId, 25_000m, "SINPE-001", "idem-001");
        payment.ReportPayment();
        repository.GetPaymentByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        repository.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var handler = new ConfirmProviderBookingPaymentCommandHandler(repository, auditLog, unitOfWork);
        var result = await handler.Handle(
            new ConfirmProviderBookingPaymentCommand(Guid.NewGuid(), payment.Id, "bank-tx-001"), default);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(ProviderPaymentStatus.Confirmed);
        booking.Status.Should().Be(ProviderBookingStatus.Confirmed);
    }
}