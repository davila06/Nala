using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicInventory;

public sealed record ClinicInventoryItemDto(
    Guid Id,
    Guid ClinicId,
    string Name,
    ClinicInventoryItemType Type,
    string Unit,
    int MinimumStock,
    int TotalAvailable,
    bool IsBelowMinimum,
    bool IsActive,
    IReadOnlyList<ClinicInventoryLotDto>? Lots = null)
{
    public static ClinicInventoryItemDto FromDomain(ClinicInventoryItem item, int totalAvailable, IReadOnlyList<ClinicInventoryLotDto>? lots = null) => new(
        item.Id,
        item.ClinicId,
        item.Name,
        item.Type,
        item.Unit,
        item.MinimumStock,
        totalAvailable,
        item.IsBelowMinimum(totalAvailable),
        item.IsActive,
        lots);
}

public sealed record ClinicInventoryLotDto(
    Guid Id,
    Guid ItemId,
    string LotNumber,
    DateOnly? ExpiresAt,
    int InitialQuantity,
    int AvailableQuantity,
    decimal UnitCostCrc,
    string? SupplierName,
    string LocationName);

public sealed record ClinicInventoryMovementDto(
    Guid Id,
    Guid ItemId,
    Guid LotId,
    int QuantityDelta,
    ClinicInventoryMovementReason Reason,
    Guid? PetId,
    Guid? ConsultationId,
    Guid? CertificateId,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record ClinicInventoryValuationDto(
    Guid ClinicId,
    decimal TotalValueCrc,
    int TotalUnits,
    IReadOnlyList<ClinicInventoryValuationLineDto> Lines,
    IReadOnlyList<ClinicInventoryLocationValueDto> ByLocation);

public sealed record ClinicInventoryValuationLineDto(
    Guid ItemId,
    string ItemName,
    ClinicInventoryItemType Type,
    int AvailableQuantity,
    decimal ValueCrc,
    bool IsBelowMinimum);

public sealed record ClinicInventoryLocationValueDto(string LocationName, int AvailableQuantity, decimal ValueCrc);

public sealed record AddClinicInventoryItemCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    string Name,
    ClinicInventoryItemType Type,
    string Unit,
    int MinimumStock)
    : IRequest<Result<ClinicInventoryItemDto>>;

public sealed class AddClinicInventoryItemCommandValidator : AbstractValidator<AddClinicInventoryItemCommand>
{
    public AddClinicInventoryItemCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(40);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddClinicInventoryItemCommandHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddClinicInventoryItemCommand, Result<ClinicInventoryItemDto>>
{
    public async Task<Result<ClinicInventoryItemDto>> Handle(AddClinicInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicInventoryItemDto>("Acceso denegado.");

        var item = ClinicInventoryItem.Create(request.ClinicId, request.Name, request.Type, request.Unit, request.MinimumStock);
        await inventoryRepository.AddItemAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicInventoryItemDto.FromDomain(item, 0));
    }
}

public sealed record ReceiveClinicInventoryLotCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid ItemId,
    string LotNumber,
    DateOnly? ExpiresAt,
    int Quantity,
    decimal UnitCostCrc,
    string? SupplierName,
    string? LocationName = null)
    : IRequest<Result<ClinicInventoryLotDto>>;

public sealed class ReceiveClinicInventoryLotCommandHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReceiveClinicInventoryLotCommand, Result<ClinicInventoryLotDto>>
{
    public async Task<Result<ClinicInventoryLotDto>> Handle(ReceiveClinicInventoryLotCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicInventoryLotDto>("Acceso denegado.");
        var item = await inventoryRepository.GetItemByIdAsync(request.ItemId, cancellationToken);
        if (item is null || item.ClinicId != request.ClinicId)
            return Result.Failure<ClinicInventoryLotDto>("Producto clínico no encontrado.");

        var lot = ClinicInventoryLot.Receive(request.ClinicId, request.ItemId, request.LotNumber, request.ExpiresAt, request.Quantity, request.UnitCostCrc, request.SupplierName, request.LocationName);
        await inventoryRepository.AddLotAsync(lot, cancellationToken);
        var movement = ClinicInventoryMovement.Create(request.ClinicId, request.ItemId, lot.Id, request.Quantity, ClinicInventoryMovementReason.StockReceived, request.ClinicUserId, null, null, null, request.SupplierName);
        await inventoryRepository.AddMovementAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new ClinicInventoryLotDto(lot.Id, lot.ItemId, lot.LotNumber, lot.ExpiresAt, lot.InitialQuantity, lot.AvailableQuantity, lot.UnitCostCrc, lot.SupplierName, lot.LocationName));
    }
}

public sealed record ConsumeClinicInventoryCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid ItemId,
    int Quantity,
    ClinicInventoryMovementReason Reason,
    Guid? PetId,
    Guid? ConsultationId,
    Guid? CertificateId)
    : IRequest<Result<IReadOnlyList<ClinicInventoryMovementDto>>>;

public sealed class ConsumeClinicInventoryCommandHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConsumeClinicInventoryCommand, Result<IReadOnlyList<ClinicInventoryMovementDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicInventoryMovementDto>>> Handle(ConsumeClinicInventoryCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Result.Failure<IReadOnlyList<ClinicInventoryMovementDto>>("La cantidad debe ser positiva.");
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<IReadOnlyList<ClinicInventoryMovementDto>>("Acceso denegado.");
        var item = await inventoryRepository.GetItemByIdAsync(request.ItemId, cancellationToken);
        if (item is null || item.ClinicId != request.ClinicId)
            return Result.Failure<IReadOnlyList<ClinicInventoryMovementDto>>("Producto clínico no encontrado.");

        var remaining = request.Quantity;
        var lots = await inventoryRepository.GetAvailableLotsByItemAsync(request.ItemId, cancellationToken);
        if (lots.Sum(lot => lot.AvailableQuantity) < request.Quantity)
            return Result.Failure<IReadOnlyList<ClinicInventoryMovementDto>>("No hay stock suficiente.");

        var movements = new List<ClinicInventoryMovement>();
        foreach (var lot in lots)
        {
            if (remaining == 0) break;
            var consume = Math.Min(remaining, lot.AvailableQuantity);
            var movement = lot.Consume(consume, request.Reason, request.ClinicUserId, request.PetId, request.ConsultationId, request.CertificateId);
            inventoryRepository.UpdateLot(lot);
            await inventoryRepository.AddMovementAsync(movement, cancellationToken);
            movements.Add(movement);
            remaining -= consume;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ClinicInventoryMovementDto>>(movements.Select(ToDto).ToList());
    }

    public static ClinicInventoryMovementDto ToDto(ClinicInventoryMovement movement) => new(
        movement.Id,
        movement.ItemId,
        movement.LotId,
        movement.QuantityDelta,
        movement.Reason,
        movement.PetId,
        movement.ConsultationId,
        movement.CertificateId,
        movement.Notes,
        movement.CreatedAt);
}

public sealed record AdjustClinicInventoryLotCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid LotId,
    int QuantityDelta,
    string Reason)
    : IRequest<Result<ClinicInventoryMovementDto>>;

public sealed class AdjustClinicInventoryLotCommandHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustClinicInventoryLotCommand, Result<ClinicInventoryMovementDto>>
{
    public async Task<Result<ClinicInventoryMovementDto>> Handle(AdjustClinicInventoryLotCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<ClinicInventoryMovementDto>("El motivo del ajuste es requerido.");
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicInventoryMovementDto>("Acceso denegado.");
        var lot = await inventoryRepository.GetLotByIdAsync(request.LotId, cancellationToken);
        if (lot is null || lot.ClinicId != request.ClinicId)
            return Result.Failure<ClinicInventoryMovementDto>("Lote no encontrado.");

        ClinicInventoryMovement movement;
        try
        {
            movement = lot.Adjust(request.QuantityDelta, request.ClinicUserId, request.Reason);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Result.Failure<ClinicInventoryMovementDto>(ex.Message);
        }

        inventoryRepository.UpdateLot(lot);
        await inventoryRepository.AddMovementAsync(movement, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicInventoryAdjusted, "ClinicInventoryLot", lot.Id.ToString(), request.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ConsumeClinicInventoryCommandHandler.ToDto(movement));
    }
}

public sealed record GetClinicInventoryQuery(Guid ClinicId, Guid ClinicUserId)
    : IRequest<Result<IReadOnlyList<ClinicInventoryItemDto>>>;

public sealed class GetClinicInventoryQueryHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository)
    : IRequestHandler<GetClinicInventoryQuery, Result<IReadOnlyList<ClinicInventoryItemDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicInventoryItemDto>>> Handle(GetClinicInventoryQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<IReadOnlyList<ClinicInventoryItemDto>>("Acceso denegado.");

        var items = await inventoryRepository.GetItemsByClinicAsync(request.ClinicId, cancellationToken);
        var result = new List<ClinicInventoryItemDto>(items.Count);
        foreach (var item in items)
        {
            var lots = await inventoryRepository.GetLotsByItemAsync(item.Id, cancellationToken);
            var lotDtos = lots.Select(lot => new ClinicInventoryLotDto(
                lot.Id,
                lot.ItemId,
                lot.LotNumber,
                lot.ExpiresAt,
                lot.InitialQuantity,
                lot.AvailableQuantity,
                lot.UnitCostCrc,
                lot.SupplierName,
                lot.LocationName)).ToList();
            result.Add(ClinicInventoryItemDto.FromDomain(item, lots.Sum(lot => lot.AvailableQuantity), lotDtos));
        }
        return Result.Success<IReadOnlyList<ClinicInventoryItemDto>>(result);
    }
}

public sealed record GetClinicInventoryValuationQuery(Guid ClinicId, Guid ClinicUserId)
    : IRequest<Result<ClinicInventoryValuationDto>>;

public sealed class GetClinicInventoryValuationQueryHandler(
    IClinicRepository clinicRepository,
    IClinicInventoryRepository inventoryRepository)
    : IRequestHandler<GetClinicInventoryValuationQuery, Result<ClinicInventoryValuationDto>>
{
    public async Task<Result<ClinicInventoryValuationDto>> Handle(GetClinicInventoryValuationQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicInventoryValuationDto>("Acceso denegado.");

        var items = await inventoryRepository.GetItemsByClinicAsync(request.ClinicId, cancellationToken);
        var lines = new List<ClinicInventoryValuationLineDto>();
        var locationValues = new Dictionary<string, (int Quantity, decimal Value)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var lots = await inventoryRepository.GetLotsByItemAsync(item.Id, cancellationToken);
            var quantity = lots.Sum(lot => lot.AvailableQuantity);
            var value = lots.Sum(lot => lot.AvailableQuantity * lot.UnitCostCrc);
            lines.Add(new ClinicInventoryValuationLineDto(item.Id, item.Name, item.Type, quantity, value, item.IsBelowMinimum(quantity)));

            foreach (var lot in lots.Where(lot => lot.AvailableQuantity > 0))
            {
                var current = locationValues.GetValueOrDefault(lot.LocationName);
                locationValues[lot.LocationName] = (current.Quantity + lot.AvailableQuantity, current.Value + lot.AvailableQuantity * lot.UnitCostCrc);
            }
        }

        var byLocation = locationValues
            .Select(pair => new ClinicInventoryLocationValueDto(pair.Key, pair.Value.Quantity, pair.Value.Value))
            .OrderBy(line => line.LocationName)
            .ToList();

        return Result.Success(new ClinicInventoryValuationDto(
            request.ClinicId,
            lines.Sum(line => line.ValueCrc),
            lines.Sum(line => line.AvailableQuantity),
            lines,
            byLocation));
    }
}
