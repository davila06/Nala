using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Queries.ExportMyData;

/// <summary>Aggregates all personal data owned by a user into a single downloadable export.</summary>
public sealed record ExportMyDataQuery(Guid UserId) : IRequest<Result<UserDataExportDto>>;
