using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.GrantHealthDataConsent;

/// <summary>
/// Records the authenticated user's explicit, differentiated consent to processing
/// of their pets' health data (Ley 8968 Art. 9 — sensitive data). Idempotent.
/// </summary>
public sealed record GrantHealthDataConsentCommand(Guid UserId) : IRequest<Result<DateTimeOffset>>;
