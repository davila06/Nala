using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Commands.SubmitAllyApplication;

public sealed class SubmitAllyApplicationCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitAllyApplicationCommand, Result<AllyProfileDto>>
{
    public async Task<Result<AllyProfileDto>> Handle(
        SubmitAllyApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AllyProfileDto>("User not found.");

        var existingProfile = await allyProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingProfile is null)
        {
            var profile = PawTrack.Domain.Allies.AllyProfile.Create(
                request.UserId,
                request.OrganizationName,
                request.AllyType,
                request.CoverageLabel,
                request.CoverageLat,
                request.CoverageLng,
                request.CoverageRadiusMetres);

            await allyProfileRepository.AddAsync(profile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(AllyProfileDto.FromDomain(profile));
        }

        existingProfile.Resubmit(
            request.OrganizationName,
            request.AllyType,
            request.CoverageLabel,
            request.CoverageLat,
            request.CoverageLng,
            request.CoverageRadiusMetres);

        allyProfileRepository.Update(existingProfile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AllyProfileDto.FromDomain(existingProfile));
    }
}