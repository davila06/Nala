using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicBilling;

public sealed record ClinicSaleLineInput(string Description, ClinicSaleLineType Type, int Quantity, decimal UnitPriceCrc, Guid? InventoryItemId, Guid? InventoryLotId);
public sealed record ClinicPaymentInput(decimal AmountCrc, ClinicPaymentMethod Method, string? Reference);

public sealed record ClinicSaleDto(Guid Id, string ReceiptNumber, string Status, decimal SubtotalCrc, decimal DiscountCrc, decimal TotalCrc, decimal PaidCrc, decimal BalanceCrc)
{
    public static ClinicSaleDto FromDomain(ClinicSale sale) => new(sale.Id, sale.ReceiptNumber, sale.Status.ToString(), sale.SubtotalCrc, sale.DiscountCrc, sale.TotalCrc, sale.PaidCrc, sale.BalanceCrc);
}

public sealed record ClinicSalePaymentDto(Guid Id, decimal AmountCrc, string Method, string? Reference, DateTimeOffset ReceivedAt);
public sealed record ClinicSaleRefundDto(Guid Id, Guid PaymentId, decimal AmountCrc, string Method, string EvidenceReference, DateTimeOffset RefundedAt);
public sealed record ClinicFiscalSubmissionDto(Guid Id, string Status, string? ProviderReference);
public sealed record SubmitClinicSaleFiscalCommand(Guid ClinicId, Guid ClinicUserId, Guid SaleId) : IRequest<Result<ClinicFiscalSubmissionDto>>;

public sealed class SubmitClinicSaleFiscalCommandHandler(
    IClinicRepository clinics, IClinicBillingRepository billing, IClinicFinanceAccessRepository financeAccess,
    IUserRepository users, IClinicFiscalIssuerRegistry issuerRegistry, IClinicFiscalGateway gateway,
    IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitClinicSaleFiscalCommand, Result<ClinicFiscalSubmissionDto>>
{
    public async Task<Result<ClinicFiscalSubmissionDto>> Handle(SubmitClinicSaleFiscalCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Fiscal, ct))
            return Result.Failure<ClinicFiscalSubmissionDto>("Acceso denegado.");
        var actor = await users.GetByIdAsync(request.ClinicUserId, ct);
        if (actor?.HasMfa != true) return Result.Failure<ClinicFiscalSubmissionDto>("MFA requerido para presentacion fiscal.");
        var sale = await billing.GetSaleByIdAsync(request.SaleId, ct);
        if (sale is null || sale.ClinicId != request.ClinicId || sale.Status != ClinicSaleStatus.Paid || sale.TotalCrc <= 0)
            return Result.Failure<ClinicFiscalSubmissionDto>("Venta pagada de esta clinica requerida.");
        var existing = await billing.GetFiscalSubmissionAsync(sale.Id, ct);
        if (existing is not null)
            return Result.Success(new ClinicFiscalSubmissionDto(existing.Id, existing.Status.ToString(), existing.ProviderReference));
        var issuerTaxId = issuerRegistry.GetVerifiedIssuerTaxId(request.ClinicId);
        if (string.IsNullOrWhiteSpace(issuerTaxId)) return Result.Failure<ClinicFiscalSubmissionDto>("Emisor fiscal de la clinica no verificado.");

        var submission = ClinicFiscalSubmission.Create(clinic.Id, sale.Id, issuerTaxId, request.ClinicUserId);
        await billing.AddFiscalSubmissionAsync(submission, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var result = await gateway.SubmitAsync(clinic.Id, sale.Id, issuerTaxId, sale.ReceiptNumber, sale.TotalCrc, ct);
        if (result.IsFailure)
        {
            submission.MarkFailed();
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<ClinicFiscalSubmissionDto>("Proveedor fiscal no confirmo recepcion; revisar antes de reintentar.");
        }
        submission.MarkSubmitted(result.Value!);
        await audit.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicFiscalSubmitted,
            "ClinicFiscalSubmission", submission.Id.ToString(), sale.ReceiptNumber), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new ClinicFiscalSubmissionDto(submission.Id, submission.Status.ToString(), submission.ProviderReference));
    }
}
public sealed record ClinicSaleLedgerDto(ClinicSaleDto Sale, IReadOnlyList<ClinicSalePaymentDto> Payments, IReadOnlyList<ClinicSaleRefundDto> Refunds);
public sealed record GetClinicSaleLedgerQuery(Guid ClinicId, Guid ClinicUserId, Guid SaleId) : IRequest<Result<ClinicSaleLedgerDto>>;

public sealed class GetClinicSaleLedgerQueryHandler(IClinicRepository clinics, IClinicBillingRepository billing, IClinicFinanceAccessRepository financeAccess)
    : IRequestHandler<GetClinicSaleLedgerQuery, Result<ClinicSaleLedgerDto>>
{
    public async Task<Result<ClinicSaleLedgerDto>> Handle(GetClinicSaleLedgerQuery request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.ViewReport, ct))
            return Result.Failure<ClinicSaleLedgerDto>("Acceso denegado.");
        var sale = await billing.GetSaleByIdAsync(request.SaleId, ct);
        if (sale is null || sale.ClinicId != request.ClinicId) return Result.Failure<ClinicSaleLedgerDto>("Venta no encontrada.");
        return Result.Success(new ClinicSaleLedgerDto(ClinicSaleDto.FromDomain(sale),
            sale.Payments.Select(payment => new ClinicSalePaymentDto(payment.Id, payment.AmountCrc, payment.Method.ToString(), payment.Reference, payment.ReceivedAt)).ToList(),
            sale.Refunds.Select(refund => new ClinicSaleRefundDto(refund.Id, refund.PaymentId, refund.AmountCrc, refund.Method.ToString(), refund.EvidenceReference, refund.RefundedAt)).ToList()));
    }
}

public sealed record CreateClinicSaleCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid? AppointmentId,
    Guid? ConsultationId,
    Guid? PetId,
    string ReceiptNumber,
    IReadOnlyList<ClinicSaleLineInput> Lines,
    decimal DiscountCrc = 0,
    string? DiscountReason = null)
    : IRequest<Result<ClinicSaleDto>>;

public sealed class CreateClinicSaleCommandValidator : AbstractValidator<CreateClinicSaleCommand>
{
    public CreateClinicSaleCommandValidator()
    {
        RuleFor(x => x.ReceiptNumber).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            line.RuleFor(x => x.Quantity).GreaterThan(0);
            line.RuleFor(x => x.UnitPriceCrc).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateClinicSaleCommandHandler(
    IClinicRepository clinicRepository,
    IClinicBillingRepository billingRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IVeterinarianAppointmentRepository appointmentRepository,
    IClinicalConsultationRepository consultationRepository,
    IClinicFinanceAccessRepository financeAccess)
    : IRequestHandler<CreateClinicSaleCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(CreateClinicSaleCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Collect, cancellationToken))
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        if (request.DiscountCrc > 0 && clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Void, cancellationToken))
            return Result.Failure<ClinicSaleDto>("Descuento requiere administrador financiero.");
        if (request.AppointmentId.HasValue)
        {
            var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId.Value, cancellationToken);
            if (appointment is null || appointment.ClinicId != request.ClinicId || request.PetId.HasValue && appointment.PetId != request.PetId)
                return Result.Failure<ClinicSaleDto>("Cita no pertenece a esta clínica o mascota.");
        }
        if (request.ConsultationId.HasValue)
        {
            var consultation = await consultationRepository.GetByIdAsync(request.ConsultationId.Value, cancellationToken);
            if (consultation is null || consultation.ClinicId != request.ClinicId
                || request.AppointmentId.HasValue && consultation.AppointmentId != request.AppointmentId
                || request.PetId.HasValue && consultation.PetId != request.PetId)
                return Result.Failure<ClinicSaleDto>("Consulta no pertenece a esta clínica, cita o mascota.");
        }
        var sale = ClinicSale.Create(request.ClinicId, request.ClinicUserId, request.AppointmentId, request.ConsultationId, request.PetId, request.ReceiptNumber);
        foreach (var line in request.Lines)
            sale.AddLine(line.Description, line.Type, line.Quantity, line.UnitPriceCrc, line.InventoryItemId, line.InventoryLotId);
        if (request.DiscountCrc > 0)
            sale.ApplyDiscount(request.DiscountCrc, request.DiscountReason ?? "Descuento aprobado", request.ClinicUserId);
        await billingRepository.AddSaleAsync(sale, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSaleCreated, "ClinicSale", sale.Id.ToString(), sale.ReceiptNumber), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed record RegisterClinicSalePaymentCommand(Guid ClinicId, Guid ClinicUserId, Guid SaleId, decimal AmountCrc, ClinicPaymentMethod Method, string? Reference)
    : IRequest<Result<ClinicSaleDto>>;

public sealed record RecordClinicSaleRefundCommand(Guid ClinicId, Guid ClinicUserId, Guid SaleId, Guid PaymentId, decimal AmountCrc, string Reason, string EvidenceReference)
    : IRequest<Result<ClinicSaleDto>>;

public sealed class RecordClinicSaleRefundCommandValidator : AbstractValidator<RecordClinicSaleRefundCommand>
{
    public RecordClinicSaleRefundCommandValidator()
    {
        RuleFor(x => x.AmountCrc).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.EvidenceReference).NotEmpty().MaximumLength(120);
    }
}

public sealed class RecordClinicSaleRefundCommandHandler(
    IClinicRepository clinics, IClinicBillingRepository billing, IClinicFinanceAccessRepository financeAccess,
    IAuditLogRepository audit, IUnitOfWork unitOfWork, IUserRepository users)
    : IRequestHandler<RecordClinicSaleRefundCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(RecordClinicSaleRefundCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Refund, ct))
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        if ((await users.GetByIdAsync(request.ClinicUserId, ct))?.HasMfa != true)
            return Result.Failure<ClinicSaleDto>("MFA requerido para devoluciones.");
        var sale = await billing.GetSaleByIdAsync(request.SaleId, ct);
        if (sale is null || sale.ClinicId != request.ClinicId) return Result.Failure<ClinicSaleDto>("Venta no encontrada.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        if (await billing.HasCashCloseAsync(request.ClinicId, today, ct))
            return Result.Failure<ClinicSaleDto>("La caja del dia esta cerrada.");
        if (await billing.GetFiscalSubmissionAsync(sale.Id, ct) is not null)
            return Result.Failure<ClinicSaleDto>("Venta presentada a proveedor fiscal; requiere nota de credito antes de devolver.");
        ClinicSaleRefund refund;
        try { refund = sale.RecordRefund(request.PaymentId, request.AmountCrc, request.Reason, request.EvidenceReference, request.ClinicUserId); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { return Result.Failure<ClinicSaleDto>(ex.Message); }
        billing.UpdateSale(sale);
        await audit.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSaleRefundRecorded,
            "ClinicSaleRefund", refund.Id.ToString(), $"{sale.Id}:{refund.AmountCrc}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed class RegisterClinicSalePaymentCommandHandler(
    IClinicRepository clinicRepository,
    IClinicBillingRepository billingRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IClinicFinanceAccessRepository financeAccess)
    : IRequestHandler<RegisterClinicSalePaymentCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(RegisterClinicSalePaymentCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Collect, cancellationToken))
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        var sale = await billingRepository.GetSaleByIdAsync(request.SaleId, cancellationToken);
        if (sale is null || sale.ClinicId != request.ClinicId)
            return Result.Failure<ClinicSaleDto>("Venta no encontrada.");
        if (await billingRepository.HasCashCloseAsync(request.ClinicId, DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6)), cancellationToken))
            return Result.Failure<ClinicSaleDto>("La caja del dia esta cerrada.");
        try { sale.RecordPayment(request.AmountCrc, request.Method, request.Reference, request.ClinicUserId); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { return Result.Failure<ClinicSaleDto>(ex.Message); }
        billingRepository.UpdateSale(sale);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSalePaymentRecorded, "ClinicSale", sale.Id.ToString(), request.Method.ToString()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed record VoidClinicSaleCommand(Guid ClinicId, Guid ClinicUserId, Guid SaleId, string Reason) : IRequest<Result<ClinicSaleDto>>;

public sealed class VoidClinicSaleCommandHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork, IClinicFinanceAccessRepository financeAccess, IUserRepository users)
    : IRequestHandler<VoidClinicSaleCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(VoidClinicSaleCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.Void, cancellationToken))
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        if ((await users.GetByIdAsync(request.ClinicUserId, cancellationToken))?.HasMfa != true)
            return Result.Failure<ClinicSaleDto>("MFA requerido para anulaciones.");
        var sale = await billingRepository.GetSaleByIdAsync(request.SaleId, cancellationToken);
        if (sale is null || sale.ClinicId != request.ClinicId)
            return Result.Failure<ClinicSaleDto>("Venta no encontrada.");
        if (await billingRepository.GetFiscalSubmissionAsync(sale.Id, cancellationToken) is not null)
            return Result.Failure<ClinicSaleDto>("Venta presentada a proveedor fiscal; requiere flujo de nota de credito.");
        try { sale.Void(request.Reason, request.ClinicUserId); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) { return Result.Failure<ClinicSaleDto>(ex.Message); }
        billingRepository.UpdateSale(sale);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSaleVoided, "ClinicSale", sale.Id.ToString(), request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed record CloseClinicCashCommand(Guid ClinicId, Guid ClinicUserId, DateOnly BusinessDate) : IRequest<Result<Guid>>;

public sealed class CloseClinicCashCommandHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork, IClinicFinanceAccessRepository financeAccess, IUserRepository users)
    : IRequestHandler<CloseClinicCashCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CloseClinicCashCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.CloseCash, cancellationToken))
            return Result.Failure<Guid>("Acceso denegado.");
        if ((await users.GetByIdAsync(request.ClinicUserId, cancellationToken))?.HasMfa != true)
            return Result.Failure<Guid>("MFA requerido para cierre de caja.");
        if (await billingRepository.HasCashCloseAsync(request.ClinicId, request.BusinessDate, cancellationToken))
            return Result.Failure<Guid>("La caja del dia ya fue cerrada.");
        var payments = await billingRepository.GetPaymentsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var refunds = await billingRepository.GetRefundsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var snapshots = payments.Select(payment => (payment.Method, payment.AmountCrc))
            .Concat(refunds.Select(refund => (refund.Method, -refund.AmountCrc)))
            .GroupBy(movement => movement.Method)
            .Select(group => new ClinicCashClosePaymentSnapshot(group.Key, group.Sum(movement => movement.Item2)))
            .ToList();
        var close = ClinicCashClose.Create(request.ClinicId, request.BusinessDate, request.ClinicUserId, snapshots);
        await billingRepository.AddCashCloseAsync(close, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCashClosed, "ClinicCashClose", close.Id.ToString(), request.BusinessDate.ToString("O")), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(close.Id);
    }
}

public sealed record ClinicSalesReportDto(decimal TotalPaidCrc, IReadOnlyDictionary<string, decimal> ByPaymentMethod, IReadOnlyDictionary<string, decimal> ByService, IReadOnlyDictionary<string, decimal> ByVeterinarian);
public sealed record GetClinicSalesReportQuery(Guid ClinicId, Guid ClinicUserId, DateOnly BusinessDate) : IRequest<Result<ClinicSalesReportDto>>;

public sealed class GetClinicSalesReportQueryHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository, IClinicFinanceAccessRepository financeAccess)
    : IRequestHandler<GetClinicSalesReportQuery, Result<ClinicSalesReportDto>>
{
    public async Task<Result<ClinicSalesReportDto>> Handle(GetClinicSalesReportQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId &&
            !await financeAccess.HasPermissionAsync(request.ClinicId, request.ClinicUserId, ClinicFinancePermission.ViewReport, cancellationToken))
            return Result.Failure<ClinicSalesReportDto>("Acceso denegado.");
        var payments = await billingRepository.GetPaymentsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var refunds = await billingRepository.GetRefundsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var sales = await billingRepository.GetSalesForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var byVeterinarian = await billingRepository.GetSalesByVeterinarianForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var byMethod = payments.Select(payment => (payment.Method, payment.AmountCrc))
            .Concat(refunds.Select(refund => (refund.Method, -refund.AmountCrc)))
            .GroupBy(movement => movement.Method.ToString()).ToDictionary(group => group.Key, group => group.Sum(movement => movement.Item2));
        var byService = sales.Where(sale => sale.Status != ClinicSaleStatus.Voided)
            .SelectMany(sale => sale.Lines)
            .GroupBy(line => line.Description)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.LineTotalCrc));
        return Result.Success(new ClinicSalesReportDto(payments.Sum(payment => payment.AmountCrc) - refunds.Sum(refund => refund.AmountCrc), byMethod, byService, byVeterinarian));
    }
}
