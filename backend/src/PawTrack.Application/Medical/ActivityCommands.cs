using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ActivityLogDto(
    Guid Id,
    DateOnly Date,
    string Type,
    int DurationMinutes,
    int? DistanceMeters,
    string? Notes,
    string Source)
{
    public static ActivityLogDto FromDomain(ActivityLog a) => new(
        a.Id, a.Date, a.Type.ToString(), a.DurationMinutes,
        a.DistanceMeters, a.Notes, a.Source.ToString());
}

public sealed record ActivityWeekSummaryDto(
    DateOnly WeekStart,
    int TotalMinutes,
    int? TotalDistanceMeters,
    int DaysActive);

public sealed record ActivityBenchmarkDto(
    int DailyMinutesMin,
    int DailyMinutesMax,
    int DailyKmMin,
    int DailyKmMax,
    string EnergyLevel);

public sealed record ActivitySummaryDto(
    IReadOnlyList<ActivityLogDto> Logs,
    IReadOnlyList<ActivityWeekSummaryDto> WeeklyTotals,
    ActivityBenchmarkDto? Benchmark,
    int StreakDays,
    int BestStreakDays);

// ── Log activity ──────────────────────────────────────────────────────────────

public sealed record LogActivityCommand(
    Guid PetId,
    Guid OwnerId,
    DateOnly Date,
    ActivityType Type,
    int DurationMinutes,
    int? DistanceMeters,
    string? Notes,
    ActivitySource Source = ActivitySource.Manual)
    : IRequest<Result<ActivityLogDto>>;

public sealed class LogActivityCommandValidator : AbstractValidator<LogActivityCommand>
{
    public LogActivityCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.DistanceMeters).GreaterThanOrEqualTo(0).When(x => x.DistanceMeters.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Date).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha no puede ser futura.");
    }
}

public sealed class LogActivityCommandHandler(
    IPetRepository petRepository,
    IActivityLogRepository activityRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogActivityCommand, Result<ActivityLogDto>>
{
    public async Task<Result<ActivityLogDto>> Handle(LogActivityCommand request, CancellationToken ct)
    {
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.OwnerId, ct);
        if (!isPlus && request.Source == ActivitySource.Manual)
            return Result.Failure<ActivityLogDto>("El registro de actividad requiere el plan Plus.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<ActivityLogDto>("Mascota no encontrada.");

        var log = ActivityLog.Record(
            request.PetId, request.OwnerId, request.Date,
            request.Type, request.DurationMinutes,
            request.DistanceMeters, request.Notes, request.Source);

        await activityRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ActivityLogDto.FromDomain(log));
    }
}

// ── Delete activity ───────────────────────────────────────────────────────────

public sealed record DeleteActivityLogCommand(Guid ActivityId, Guid OwnerId)
    : IRequest<Result<Unit>>;

public sealed class DeleteActivityLogCommandHandler(
    IActivityLogRepository activityRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteActivityLogCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteActivityLogCommand request, CancellationToken ct)
    {
        var log = await activityRepository.GetByIdAsync(request.ActivityId, ct);
        if (log is null) return Result.Failure<Unit>("Registro no encontrado.");
        if (log.OwnerId != request.OwnerId) return Result.Failure<Unit>("Acceso denegado.");

        activityRepository.Delete(log);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Get activity summary ──────────────────────────────────────────────────────

public sealed record GetActivityLogsQuery(
    Guid PetId,
    Guid RequestingUserId,
    DateOnly? From,
    DateOnly? To)
    : IRequest<Result<ActivitySummaryDto>>;

public sealed class GetActivityLogsQueryHandler(
    IPetRepository petRepository,
    IActivityLogRepository activityRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetActivityLogsQuery, Result<ActivitySummaryDto>>
{
    public async Task<Result<ActivitySummaryDto>> Handle(GetActivityLogsQuery request, CancellationToken ct)
    {
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.RequestingUserId, ct);
        if (!isPlus)
            return Result.Failure<ActivitySummaryDto>("El historial de actividad requiere el plan Plus.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<ActivitySummaryDto>("Mascota no encontrada.");

        var canAccess = pet.OwnerId == request.RequestingUserId
            || (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId);
        if (!canAccess) return Result.Failure<ActivitySummaryDto>("Acceso denegado.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? today.AddDays(-30);
        var to   = request.To   ?? today;

        var logs = await activityRepository.GetByPetAndDateRangeAsync(request.PetId, from, to, ct);

        // Weekly totals — group by week start (Monday)
        var weeklyTotals = logs
            .GroupBy(l => l.Date.AddDays(-(int)l.Date.DayOfWeek == 0 ? 6 : (int)l.Date.DayOfWeek - 1))
            .OrderBy(g => g.Key)
            .Select(g => new ActivityWeekSummaryDto(
                g.Key,
                g.Sum(l => l.DurationMinutes),
                g.Any(l => l.DistanceMeters.HasValue) ? g.Sum(l => l.DistanceMeters ?? 0) : null,
                g.Select(l => l.Date).Distinct().Count()))
            .ToList()
            .AsReadOnly();

        // Streak: count consecutive days with at least one log ending at today
        var loggedDates = logs.Select(l => l.Date).Distinct().OrderByDescending(d => d).ToHashSet();
        var streak = 0;
        var best = 0;
        var current = today;
        while (loggedDates.Contains(current)) { streak++; current = current.AddDays(-1); }
        best = streak;

        var benchmark = BreedActivityBenchmark.Resolve(pet.Breed, pet.Species.ToString());
        var benchmarkDto = benchmark is null ? null
            : new ActivityBenchmarkDto(
                benchmark.DailyMinutesMin, benchmark.DailyMinutesMax,
                benchmark.DailyKmMin, benchmark.DailyKmMax,
                benchmark.EnergyLevel);

        var dtos = logs.Select(ActivityLogDto.FromDomain).ToList().AsReadOnly();
        return Result.Success(new ActivitySummaryDto(dtos, weeklyTotals, benchmarkDto, streak, best));
    }
}
