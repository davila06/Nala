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
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateClinicSaleCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(CreateClinicSaleCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
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

public sealed class RegisterClinicSalePaymentCommandHandler(
    IClinicRepository clinicRepository,
    IClinicBillingRepository billingRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterClinicSalePaymentCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(RegisterClinicSalePaymentCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        var sale = await billingRepository.GetSaleByIdAsync(request.SaleId, cancellationToken);
        if (sale is null || sale.ClinicId != request.ClinicId)
            return Result.Failure<ClinicSaleDto>("Venta no encontrada.");
        try { sale.RecordPayment(request.AmountCrc, request.Method, request.Reference, request.ClinicUserId); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { return Result.Failure<ClinicSaleDto>(ex.Message); }
        billingRepository.UpdateSale(sale);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSalePaymentRecorded, "ClinicSale", sale.Id.ToString(), request.Method.ToString()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed record VoidClinicSaleCommand(Guid ClinicId, Guid ClinicUserId, Guid SaleId, string Reason) : IRequest<Result<ClinicSaleDto>>;

public sealed class VoidClinicSaleCommandHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<VoidClinicSaleCommand, Result<ClinicSaleDto>>
{
    public async Task<Result<ClinicSaleDto>> Handle(VoidClinicSaleCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicSaleDto>("Acceso denegado.");
        var sale = await billingRepository.GetSaleByIdAsync(request.SaleId, cancellationToken);
        if (sale is null || sale.ClinicId != request.ClinicId)
            return Result.Failure<ClinicSaleDto>("Venta no encontrada.");
        sale.Void(request.Reason, request.ClinicUserId);
        billingRepository.UpdateSale(sale);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicSaleVoided, "ClinicSale", sale.Id.ToString(), request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicSaleDto.FromDomain(sale));
    }
}

public sealed record CloseClinicCashCommand(Guid ClinicId, Guid ClinicUserId, DateOnly BusinessDate) : IRequest<Result<Guid>>;

public sealed class CloseClinicCashCommandHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CloseClinicCashCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CloseClinicCashCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<Guid>("Acceso denegado.");
        var payments = await billingRepository.GetPaymentsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var snapshots = payments
            .GroupBy(payment => payment.Method)
            .Select(group => new ClinicCashClosePaymentSnapshot(group.Key, group.Sum(payment => payment.AmountCrc)))
            .ToList();
        var close = ClinicCashClose.Create(request.ClinicId, request.BusinessDate, request.ClinicUserId, snapshots);
        await billingRepository.AddCashCloseAsync(close, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCashClosed, "ClinicCashClose", close.Id.ToString(), request.BusinessDate.ToString("O")), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(close.Id);
    }
}

public sealed record ClinicSalesReportDto(decimal TotalPaidCrc, IReadOnlyDictionary<string, decimal> ByPaymentMethod, IReadOnlyDictionary<string, decimal> ByService);
public sealed record GetClinicSalesReportQuery(Guid ClinicId, Guid ClinicUserId, DateOnly BusinessDate) : IRequest<Result<ClinicSalesReportDto>>;

public sealed class GetClinicSalesReportQueryHandler(IClinicRepository clinicRepository, IClinicBillingRepository billingRepository)
    : IRequestHandler<GetClinicSalesReportQuery, Result<ClinicSalesReportDto>>
{
    public async Task<Result<ClinicSalesReportDto>> Handle(GetClinicSalesReportQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicSalesReportDto>("Acceso denegado.");
        var payments = await billingRepository.GetPaymentsForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var sales = await billingRepository.GetSalesForClinicOnDateAsync(request.ClinicId, request.BusinessDate, cancellationToken);
        var byMethod = payments.GroupBy(payment => payment.Method.ToString()).ToDictionary(group => group.Key, group => group.Sum(payment => payment.AmountCrc));
        var byService = sales
            .SelectMany(sale => sale.Lines)
            .GroupBy(line => line.Description)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.LineTotalCrc));
        return Result.Success(new ClinicSalesReportDto(payments.Sum(payment => payment.AmountCrc), byMethod, byService));
    }
}
