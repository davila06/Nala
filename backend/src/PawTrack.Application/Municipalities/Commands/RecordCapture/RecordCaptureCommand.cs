using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Commands.RecordCapture;

public sealed record RecordCaptureCommand(
    Guid RecordedByUserId,
    string Canton,
    string Species,
    string Color,
    string? Breed,
    string? EstimatedAge,
    string? Notes,
    string? CollarChipNumber,
    DateTimeOffset? CapturedAt,
    string? IdempotencyKey = null) : IRequest<Result<CapturedAnimalDto>>;

public sealed class RecordCaptureCommandValidator : AbstractValidator<RecordCaptureCommand>
{
    public RecordCaptureCommandValidator()
    {
        RuleFor(x => x.Canton).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Species).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RecordCaptureCommandHandler(
    ICapturedAnimalRepository repository,
    IUnitOfWork unitOfWork,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<RecordCaptureCommand, Result<CapturedAnimalDto>>
{
    public async Task<Result<CapturedAnimalDto>> Handle(
        RecordCaptureCommand request,
        CancellationToken cancellationToken)
    {
        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                request.RecordedByUserId,
                "MaxCapturesPerYear",
                1m,
                new EntitlementContext("municipality-capture"),
                cancellationToken);
            if (!decision.Allowed)
                return Result.Failure<CapturedAnimalDto>("La municipalidad alcanzó la cuota anual de capturas.");
        }

        var animal = CapturedAnimal.Record(
            request.RecordedByUserId,
            request.Canton,
            request.Species,
            request.Color,
            request.Breed,
            request.EstimatedAge,
            request.Notes,
            request.CollarChipNumber,
            request.CapturedAt);

        if (entitlementService is not null)
        {
            var consumption = await entitlementService.ConsumeAsync(
                request.RecordedByUserId,
                "MaxCapturesPerYear",
                1m,
                request.IdempotencyKey ?? $"municipality-capture:{Guid.NewGuid():N}",
                new EntitlementContext("municipality-capture"),
                cancellationToken);
            if (!consumption.Consumed)
                return Result.Failure<CapturedAnimalDto>("La municipalidad alcanzó la cuota anual de capturas.");
        }

        await repository.AddAsync(animal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CapturedAnimalDto.FromDomain(animal));
    }
}
