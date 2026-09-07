using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderServiceAvailabilitySlotDto(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int AvailableCapacity);

public sealed record GetAvailableProviderServiceSlotsQuery(Guid ProviderServiceId, DateOnly Date)
    : IRequest<Result<IReadOnlyList<ProviderServiceAvailabilitySlotDto>>>;

public sealed class GetAvailableProviderServiceSlotsQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetAvailableProviderServiceSlotsQuery, Result<IReadOnlyList<ProviderServiceAvailabilitySlotDto>>>
{
    private static readonly TimeSpan CostaRicaOffset = TimeSpan.FromHours(-6);

    public async Task<Result<IReadOnlyList<ProviderServiceAvailabilitySlotDto>>> Handle(
        GetAvailableProviderServiceSlotsQuery request,
        CancellationToken ct)
    {
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (service is null || service.Status != ProviderServiceStatus.Published)
            return Result.Failure<IReadOnlyList<ProviderServiceAvailabilitySlotDto>>("Servicio no disponible.");

        var dayStart = new DateTimeOffset(request.Date.ToDateTime(TimeOnly.MinValue), CostaRicaOffset);
        var dayEnd = dayStart.AddDays(1);
        var rules = await repository.GetActiveAvailabilityRulesAsync(service.Id, ct);
        var bookings = await repository.GetBookingsByServiceRangeAsync(service.Id, dayStart, dayEnd, ct);
        var blocks = await repository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, dayStart, dayEnd, ct);
        var slots = new List<ProviderServiceAvailabilitySlotDto>();

        foreach (var rule in rules.Where(rule => rule.DayOfWeek == request.Date.DayOfWeek))
        {
            var slotStart = new DateTimeOffset(request.Date.ToDateTime(rule.StartsAtLocalTime), CostaRicaOffset);
            var ruleEnd = new DateTimeOffset(request.Date.ToDateTime(rule.EndsAtLocalTime), CostaRicaOffset);
            while (slotStart.AddMinutes(service.DurationMinutes) <= ruleEnd)
            {
                var slotEnd = slotStart.AddMinutes(service.DurationMinutes);
                if (blocks.Any(block => block.Blocks(slotStart, slotEnd)))
                {
                    slotStart = slotEnd;
                    continue;
                }
                var reserved = bookings
                    .Where(booking => booking.StartsAt < slotEnd && slotStart < booking.EndsAt)
                    .Sum(booking => booking.Quantity);
                if (reserved < service.Capacity)
                    slots.Add(new ProviderServiceAvailabilitySlotDto(slotStart, slotEnd, service.Capacity - reserved));
                slotStart = slotEnd;
            }
        }

        return Result.Success<IReadOnlyList<ProviderServiceAvailabilitySlotDto>>(slots);
    }
}