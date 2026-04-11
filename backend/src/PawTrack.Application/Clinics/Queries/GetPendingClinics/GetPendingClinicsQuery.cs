using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetPendingClinics;

public sealed record GetPendingClinicsQuery : IRequest<Result<IReadOnlyList<ClinicDto>>>;
