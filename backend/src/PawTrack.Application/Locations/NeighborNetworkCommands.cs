using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Locations;
using System.Text.RegularExpressions;

namespace PawTrack.Application.Locations;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record NeighborStatusDto(
    bool IsEnrolled,
    bool IsActive,
    string? Phone,
    int RadiusMeters,
    int NeighborsInRange);

// ── Enroll or update ─────────────────────────────────────────────────────────

public sealed record EnrollNeighborAlertCommand(
    Guid UserId,
    string Phone,
    int RadiusMeters = 500)
    : IRequest<Result<NeighborStatusDto>>;

public sealed class EnrollNeighborAlertCommandValidator : AbstractValidator<EnrollNeighborAlertCommand>
{
    private static readonly Regex CrPhone = new(@"^(\+506\s?)?\d{4}[-\s]?\d{4}$", RegexOptions.Compiled);

    public EnrollNeighborAlertCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty().Must(p => CrPhone.IsMatch(p.Trim()))
            .WithMessage("Número inválido. Usa el formato +506 XXXX-XXXX o 8 dígitos.");
        RuleFor(x => x.RadiusMeters).InclusiveBetween(100, 2000);
    }
}

public sealed class EnrollNeighborAlertCommandHandler(
    INeighborAlertRepository repository,
    IUserLocationRepository locationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<EnrollNeighborAlertCommand, Result<NeighborStatusDto>>
{
    public async Task<Result<NeighborStatusDto>> Handle(
        EnrollNeighborAlertCommand request, CancellationToken ct)
    {
        // Prefer the user's stored location; fall back to (9.935, -84.082) = San José center
        var loc = await locationRepository.GetByUserIdAsync(request.UserId, ct);
        var lat = (decimal)(loc?.Lat ?? 9.935);
        var lng = (decimal)(loc?.Lng ?? -84.082);

        var existing = await repository.GetByUserIdAsync(request.UserId, ct);
        if (existing is null)
        {
            var alert = NeighborAlert.Enroll(request.UserId, request.Phone, lat, lng, request.RadiusMeters);
            await repository.AddAsync(alert, ct);
        }
        else
        {
            existing.UpdatePhone(request.Phone);
            existing.SetRadius(request.RadiusMeters);
            existing.UpdateLocation(lat, lng);
            existing.Activate();
            repository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(ct);

        var count = await repository.CountActiveInRadiusAsync((double)lat, (double)lng, request.RadiusMeters, ct);
        return Result.Success(new NeighborStatusDto(true, true, request.Phone, request.RadiusMeters, count));
    }
}

// ── Update settings (radius + active toggle) ─────────────────────────────────

public sealed record UpdateNeighborSettingsCommand(
    Guid UserId,
    int RadiusMeters,
    bool IsActive)
    : IRequest<Result<Unit>>;

public sealed class UpdateNeighborSettingsCommandValidator : AbstractValidator<UpdateNeighborSettingsCommand>
{
    public UpdateNeighborSettingsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RadiusMeters).InclusiveBetween(100, 2000);
    }
}

public sealed class UpdateNeighborSettingsCommandHandler(
    INeighborAlertRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateNeighborSettingsCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        UpdateNeighborSettingsCommand request, CancellationToken ct)
    {
        var alert = await repository.GetByUserIdAsync(request.UserId, ct);
        if (alert is null)
            return Result.Failure<Unit>("No estás inscrito en la Guardia Vecinal.");

        alert.SetRadius(request.RadiusMeters);
        if (request.IsActive) alert.Activate(); else alert.Deactivate();
        repository.Update(alert);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Get status ────────────────────────────────────────────────────────────────

public sealed record GetNeighborStatusQuery(Guid UserId)
    : IRequest<Result<NeighborStatusDto>>;

public sealed class GetNeighborStatusQueryHandler(
    INeighborAlertRepository repository)
    : IRequestHandler<GetNeighborStatusQuery, Result<NeighborStatusDto>>
{
    public async Task<Result<NeighborStatusDto>> Handle(
        GetNeighborStatusQuery request, CancellationToken ct)
    {
        var alert = await repository.GetByUserIdAsync(request.UserId, ct);
        if (alert is null)
            return Result.Success(new NeighborStatusDto(false, false, null, 500, 0));

        var count = await repository.CountActiveInRadiusAsync(
            (double)alert.Lat, (double)alert.Lng, alert.RadiusMeters, ct);

        return Result.Success(new NeighborStatusDto(
            true, alert.IsActive, alert.Phone, alert.RadiusMeters, count));
    }
}

// ── Count neighbors in area (public — used in ReportLost UX hint) ─────────────

public sealed record GetNeighborCountInAreaQuery(double Lat, double Lng, int RadiusMeters = 500)
    : IRequest<Result<int>>;

public sealed class GetNeighborCountInAreaQueryHandler(INeighborAlertRepository repository)
    : IRequestHandler<GetNeighborCountInAreaQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetNeighborCountInAreaQuery request, CancellationToken ct)
    {
        var count = await repository.CountActiveInRadiusAsync(
            request.Lat, request.Lng, request.RadiusMeters, ct);
        return Result.Success(count);
    }
}
