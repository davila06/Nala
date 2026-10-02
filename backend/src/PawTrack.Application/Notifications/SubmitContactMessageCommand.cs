using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Notifications;

public sealed record SubmitContactMessageCommand(
    string Name,
    string Email,
    string Topic,
    string Message,
    string Website) : IRequest<Result<bool>>;

public sealed class SubmitContactMessageCommandValidator : AbstractValidator<SubmitContactMessageCommand>
{
    private static readonly string[] AllowedTopics = ["Consulta general", "Problema técnico", "Consulta comercial"];

    public SubmitContactMessageCommandValidator()
    {
        RuleFor(command => command.Name).MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Topic)
            .Must(topic => AllowedTopics.Contains(topic, StringComparer.Ordinal))
            .WithMessage("Selecciona un tema válido.");
        RuleFor(command => command.Message).NotEmpty().MinimumLength(20).MaximumLength(2000);
        RuleFor(command => command.Website).MaximumLength(300);
    }
}

public sealed class SubmitContactMessageCommandHandler(IContactEmailSender emailSender)
    : IRequestHandler<SubmitContactMessageCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SubmitContactMessageCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
            return Result.Success(true);

        var accepted = await emailSender.SendAsync(new ContactEmailMessage(
            request.Name.Trim(), request.Email.Trim(), request.Topic, request.Message.Trim()), cancellationToken);
        return accepted
            ? Result.Success(true)
            : Result.Failure<bool>("No fue posible enviar el mensaje. Inténtalo de nuevo más tarde.");
    }
}
