using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.DeleteAccount;

public sealed record DeleteAccountCommand(
    Guid UserId,
    string ConfirmPassword) : IRequest<Result<bool>>;
