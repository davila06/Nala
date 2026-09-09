using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Safety;

namespace PawTrack.Application.Safety.Commands.CreateAnonymousContactRequest;

public sealed record CreateAnonymousContactRequestCommand(
    Guid LostPetEventId,
    string? FinderName,
    string Message) : IRequest<Result<Guid>>;

public sealed class CreateAnonymousContactRequestCommandValidator
    : AbstractValidator<CreateAnonymousContactRequestCommand>
{
    public CreateAnonymousContactRequestCommandValidator()
    {
        RuleFor(x => x.LostPetEventId).NotEmpty();
        RuleFor(x => x.FinderName).MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(800);
    }
}

public sealed class CreateAnonymousContactRequestCommandHandler(
    ILostPetRepository lostPetRepository,
    IUserRepository userRepository,
    IAnonymousContactRequestRepository contactRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAnonymousContactRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateAnonymousContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);
        if (lostEvent is null || lostEvent.Status != Domain.LostPets.LostPetStatus.Active)
            return Result.Failure<Guid>("El reporte de pérdida no está activo.");

        var owner = await userRepository.GetByIdAsync(lostEvent.OwnerId, cancellationToken);
        if (owner is null) return Result.Failure<Guid>("El propietario no está disponible.");

        var contact = AnonymousContactRequest.Create(
            request.LostPetEventId,
            lostEvent.OwnerId,
            request.FinderName,
            request.Message);
        await contactRepository.AddAsync(contact, cancellationToken);

        var senderLabel = string.IsNullOrWhiteSpace(request.FinderName) ? "Una persona" : request.FinderName.Trim();
        var notification = Notification.Create(
            owner.Id,
            NotificationType.SystemMessage,
            "Alguien quiere ayudarte con tu mascota perdida",
            $"{senderLabel} dejó un mensaje: {request.Message.Trim()}",
            request.LostPetEventId.ToString());
        await notificationRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(contact.Id);
    }
}