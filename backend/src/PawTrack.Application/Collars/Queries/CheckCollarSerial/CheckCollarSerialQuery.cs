using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.CheckCollarSerial;

public sealed record CheckCollarSerialQuery(string Serial) : IRequest<Result<CollarSerialStatusDto>>;

public sealed record CollarSerialStatusDto(bool Available, string Status);

public sealed class CheckCollarSerialQueryHandler(ICollarTagRepository collarTagRepository)
    : IRequestHandler<CheckCollarSerialQuery, Result<CollarSerialStatusDto>>
{
    public async Task<Result<CollarSerialStatusDto>> Handle(
        CheckCollarSerialQuery request, CancellationToken cancellationToken)
    {
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null)
            return Result.Failure<CollarSerialStatusDto>("Serial no encontrado.");

        return Result.Success(new CollarSerialStatusDto(tag.IsAvailable, tag.Status.ToString()));
    }
}
