using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ReviewClinic;

/// <summary>
/// Approve or suspend a pending clinic registration.
/// </summary>
public sealed record ReviewClinicCommand(Guid ClinicId, bool Approve) : IRequest<Result<bool>>;
