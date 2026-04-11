using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(
    Guid UserId,
    string RefreshToken,
    /// <summary>
    /// The <c>jti</c> claim from the current access token.
    /// When present the jti is added to the blocklist so the access token
    /// cannot be reused within its remaining lifetime after logout.
    /// </summary>
    string? AccessTokenJti,
    DateTimeOffset? AccessTokenExpiresAt) : IRequest<Result<bool>>;
