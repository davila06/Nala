using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Payments;
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

    [Fact]
    public async Task CreatePayment_RejectsCustomerFromAnotherBooking()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var paymentService = Substitute.For<IPaymentService>();
        var paymentGateway = Substitute.For<IProviderPaymentGateway>();
        var customerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), customerUserId, Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        repository.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var handler = new CreateProviderBookingPaymentCommandHandler(repository, unitOfWork, paymentService, paymentGateway);
        var result = await handler.Handle(
            new CreateProviderBookingPaymentCommand(attackerUserId, booking.Id, "idem-attacker"), default);

        result.IsFailure.Should().BeTrue();
        await paymentGateway.DidNotReceive().CreateIntentAsync(Arg.Any<ProviderPaymentIntentRequest>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddPaymentAsync(Arg.Any<ProviderPayment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_UsesBookingTotalIncludingTaxAndPlatformFee()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var paymentService = Substitute.For<IPaymentService>();
        var paymentGateway = Substitute.For<IProviderPaymentGateway>();
        var customerId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var booking = ProviderBooking.Request(
            providerId, Guid.NewGuid(), customerId, Guid.NewGuid(), "Consulta",
            DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 2, null, taxCrc: 5200m, platformFeeCrc: 1000m);
        repository.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        repository.GetPaymentByBookingAsync(booking.Id, Arg.Any<CancellationToken>()).Returns((ProviderPayment?)null);
        paymentService.GenerateReference().Returns("PROV-001");
        paymentGateway.CreateIntentAsync(Arg.Any<ProviderPaymentIntentRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ProviderPaymentIntentRequest>();
                return new ProviderPaymentIntentResult(
                    ProviderPaymentIntentStatus.Pending, request.AmountCrc, request.Currency,
                    request.PaymentReference, null, null);
            });

        var handler = new CreateProviderBookingPaymentCommandHandler(repository, unitOfWork, paymentService, paymentGateway);
        var result = await handler.Handle(
            new CreateProviderBookingPaymentCommand(customerId, booking.Id, "idem-provider-total"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountCrc.Should().Be(booking.TotalCrc);
        await paymentGateway.Received(1).CreateIntentAsync(
            Arg.Is<ProviderPaymentIntentRequest>(request => request.AmountCrc == booking.TotalCrc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportPayment_RejectsPaymentOwnedByAnotherCustomer()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), customerUserId, Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        var payment = ProviderPayment.Create(
            booking.Id, customerUserId, booking.ServiceProviderId, 25_000m, "SINPE-002", "idem-002");
        repository.GetPaymentByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = new ReportProviderBookingPaymentCommandHandler(repository, auditLog, unitOfWork);
        var result = await handler.Handle(
            new ReportProviderBookingPaymentCommand(attackerUserId, payment.Id), default);

        result.IsFailure.Should().BeTrue();
        payment.Status.Should().NotBe(ProviderPaymentStatus.Reported);
        repository.DidNotReceive().UpdatePayment(Arg.Any<ProviderPayment>());
    }
}

public sealed class RecordManualProviderRefundCommandTests
{
    [Fact]
    public void ValidatorRejectsAmountWithMoreThanTwoDecimalPlaces()
    {
        var validator = new RecordManualProviderRefundCommandValidator();
        var command = new RecordManualProviderRefundCommand(
            Guid.NewGuid(), Guid.NewGuid(), 0.001m, "BANK-REFUND-1", "Devolución", "refund-key");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RejectsPaymentLinkedToGatewayIntent()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var operationRepository = Substitute.For<IPaymentOperationRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var payment = ProviderPayment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20_000m, "CARD-REF", "idem-card");
        var paymentIntentId = Guid.NewGuid();
        payment.BeginCardPayment(paymentIntentId);
        payment.ConfirmCardPayment(paymentIntentId, "GATEWAY-TX-1");
        repository.GetPaymentByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = new RecordManualProviderRefundCommandHandler(repository, operationRepository, auditLog, unitOfWork);
        var result = await handler.Handle(new RecordManualProviderRefundCommand(
            Guid.NewGuid(), payment.Id, 5_000m, "BANK-REFUND-1", "Devolución parcial", "card-refund-key"), default);

        result.IsFailure.Should().BeTrue();
        payment.RefundedAmountCrc.Should().Be(0);
        await operationRepository.DidNotReceive().AddAsync(Arg.Any<PaymentOperation>(), Arg.Any<CancellationToken>());
        await auditLog.DidNotReceive().AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordsExternalSinpeRefundIdempotentlyWithAudit()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var operationRepository = Substitute.For<IPaymentOperationRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var payment = ProviderPayment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20_000m, "SINPE-REFUND", "idem-refund");
        PaymentOperation? recordedOperation = null;
        payment.ReportPayment();
        payment.Confirm("BANK-PAYMENT-1");
        repository.GetPaymentByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        operationRepository.GetByIdempotencyKeyAsync(
            PaymentOperationType.Refund, "store-refund-key", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(recordedOperation));
        operationRepository.AddAsync(
            Arg.Do<PaymentOperation>(operation => recordedOperation = operation), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new RecordManualProviderRefundCommandHandler(repository, operationRepository, auditLog, unitOfWork);
        var command = new RecordManualProviderRefundCommand(
            Guid.NewGuid(), payment.Id, 5_000m, "BANK-REFUND-1", "Devolución parcial", "store-refund-key");
        var result = await handler.Handle(command, default);
        var retry = await handler.Handle(command, default);
        var conflictingRetry = await handler.Handle(command with { AmountCrc = 6_000m }, default);

        result.IsSuccess.Should().BeTrue();
        retry.IsSuccess.Should().BeTrue();
        conflictingRetry.IsFailure.Should().BeTrue();
        payment.RefundedAmountCrc.Should().Be(5_000m);
        payment.Status.Should().Be(ProviderPaymentStatus.PartiallyRefunded);
        payment.RefundReference.Should().Be("BANK-REFUND-1");
        await operationRepository.Received(1).AddAsync(Arg.Any<PaymentOperation>(), Arg.Any<CancellationToken>());
        await auditLog.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ProviderPaymentRefunded),
            Arg.Any<CancellationToken>());
    }
}
