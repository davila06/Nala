using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Application.ServiceProviders.Payments;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderPaymentDto(
    Guid Id,
    Guid BookingId,
    decimal AmountCrc,
    string Currency,
    string PaymentReference,
    string Status,
    string IdempotencyKey,
    string? ExternalReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? RefundedAt)
{
    public static ProviderPaymentDto FromDomain(ProviderPayment payment) => new(
        payment.Id, payment.BookingId, payment.AmountCrc, payment.Currency,
        payment.PaymentReference, payment.Status.ToString(), payment.IdempotencyKey,
        payment.ExternalReference, payment.CreatedAt, payment.ConfirmedAt, payment.RefundedAt);
}

public sealed record CreateProviderBookingPaymentCommand(
    Guid CustomerUserId,
    Guid BookingId,
    string IdempotencyKey) : IRequest<Result<ProviderPaymentDto>>;

public sealed class CreateProviderBookingPaymentCommandValidator : AbstractValidator<CreateProviderBookingPaymentCommand>
{
    public CreateProviderBookingPaymentCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateProviderBookingPaymentCommandHandler(
    IServiceProviderRepository repository,
    IUnitOfWork unitOfWork,
    IPaymentService paymentService,
    IProviderPaymentGateway paymentGateway)
    : IRequestHandler<CreateProviderBookingPaymentCommand, Result<ProviderPaymentDto>>
{
    public async Task<Result<ProviderPaymentDto>> Handle(CreateProviderBookingPaymentCommand request, CancellationToken ct)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();
        var existingByKey = await repository.GetPaymentByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existingByKey is not null)
            return existingByKey.CustomerUserId == request.CustomerUserId
                ? Result.Success(ProviderPaymentDto.FromDomain(existingByKey))
                : Result.Failure<ProviderPaymentDto>("No se pudo crear el pago.");

        var booking = await repository.GetBookingByIdAsync(request.BookingId, ct);
        if (booking is null || booking.CustomerUserId != request.CustomerUserId)
            return Result.Failure<ProviderPaymentDto>("Reserva no encontrada.");
        if (booking.Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.AwaitingPayment))
            return Result.Failure<ProviderPaymentDto>("La reserva no admite un pago nuevo.");

        var existingByBooking = await repository.GetPaymentByBookingAsync(booking.Id, ct);
        if (existingByBooking is not null)
            return Result.Success(ProviderPaymentDto.FromDomain(existingByBooking));

        var paymentReference = paymentService.GenerateReference();
        var amount = booking.PriceCrc * booking.Quantity;
        var intent = await paymentGateway.CreateIntentAsync(new ProviderPaymentIntentRequest(
            booking.Id, amount, "CRC", paymentReference, idempotencyKey), ct);
        if (intent.Status == ProviderPaymentIntentStatus.Failed)
            return Result.Failure<ProviderPaymentDto>(intent.FailureReason ?? "No fue posible iniciar el pago.");

        var payment = ProviderPayment.Create(
            booking.Id, booking.CustomerUserId, booking.ServiceProviderId,
            intent.AmountCrc, intent.PaymentReference, idempotencyKey);
        if (booking.Status == ProviderBookingStatus.Requested)
            booking.MarkAwaitingPayment();
        await repository.AddPaymentAsync(payment, ct);
        repository.UpdateBooking(booking);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderPaymentDto.FromDomain(payment));
    }
}

public sealed record ConfirmProviderBookingPaymentCommand(
    Guid AdminUserId,
    Guid PaymentId,
    string ExternalReference) : IRequest<Result<ProviderPaymentDto>>;

public sealed class ConfirmProviderBookingPaymentCommandValidator : AbstractValidator<ConfirmProviderBookingPaymentCommand>
{
    public ConfirmProviderBookingPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(200);
    }
}

public sealed record ReportProviderBookingPaymentCommand(Guid CustomerUserId, Guid PaymentId)
    : IRequest<Result<ProviderPaymentDto>>;

public sealed class ReportProviderBookingPaymentCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportProviderBookingPaymentCommand, Result<ProviderPaymentDto>>
{
    public async Task<Result<ProviderPaymentDto>> Handle(ReportProviderBookingPaymentCommand request, CancellationToken ct)
    {
        var payment = await repository.GetPaymentByIdAsync(request.PaymentId, ct);
        if (payment is null || payment.CustomerUserId != request.CustomerUserId)
            return Result.Failure<ProviderPaymentDto>("Pago no encontrado.");
        try
        {
            payment.ReportPayment();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<ProviderPaymentDto>(exception.Message);
        }

        repository.UpdatePayment(payment);
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.CustomerUserId, AuditAction.ProviderPaymentReported,
            "ProviderPayment", payment.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderPaymentDto.FromDomain(payment));
    }
}

public sealed class ConfirmProviderBookingPaymentCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmProviderBookingPaymentCommand, Result<ProviderPaymentDto>>
{
    public async Task<Result<ProviderPaymentDto>> Handle(ConfirmProviderBookingPaymentCommand request, CancellationToken ct)
    {
        var payment = await repository.GetPaymentByIdAsync(request.PaymentId, ct);
        if (payment is null) return Result.Failure<ProviderPaymentDto>("Pago no encontrado.");
        var booking = await repository.GetBookingByIdAsync(payment.BookingId, ct);
        if (booking is null) return Result.Failure<ProviderPaymentDto>("Reserva no encontrada.");
        try
        {
            payment.Confirm(request.ExternalReference);
            if (booking.Status == ProviderBookingStatus.AwaitingPayment)
                booking.Confirm();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Result.Failure<ProviderPaymentDto>(exception.Message);
        }

        repository.UpdatePayment(payment);
        repository.UpdateBooking(booking);
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.AdminUserId, AuditAction.ProviderPaymentConfirmed,
            "ProviderPayment", payment.Id.ToString(), request.ExternalReference), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderPaymentDto.FromDomain(payment));
    }
}