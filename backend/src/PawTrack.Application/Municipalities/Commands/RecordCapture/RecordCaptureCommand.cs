using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Commands.RecordCapture;

public sealed record RecordCaptureCommand(
    Guid    RecordedByUserId,
    string  Canton,
    string  Species,
    string  Color,
    string? Breed,
    string? EstimatedAge,
    string? Notes,
    string? CollarChipNumber,
    DateTimeOffset? CapturedAt) : IRequest<Result<CapturedAnimalDto>>;

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
    IUnitOfWork               unitOfWork)
    : IRequestHandler<RecordCaptureCommand, Result<CapturedAnimalDto>>
{
    public async Task<Result<CapturedAnimalDto>> Handle(
        RecordCaptureCommand request,
        CancellationToken cancellationToken)
    {
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

        await repository.AddAsync(animal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CapturedAnimalDto.FromDomain(animal));
    }
}
