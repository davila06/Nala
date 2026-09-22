using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Audit;

namespace PawTrack.Application.Clinics.Commands.ManageWidgetDomain;

public sealed record ClinicWidgetDomainDto(
    Guid Id, Guid ClinicId, string Domain, bool IsActive, DateTimeOffset CreatedAt)
{
    public static ClinicWidgetDomainDto FromDomain(ClinicWidgetDomain domain) =>
        new(domain.Id, domain.ClinicId, domain.Domain, domain.IsActive, domain.CreatedAt);
}

public sealed record AddClinicWidgetDomainCommand(Guid ClinicId, string Domain, Guid ActorId = default)
    : IRequest<Result<ClinicWidgetDomainDto>>;

public sealed class AddClinicWidgetDomainCommandHandler(
    IClinicWidgetDomainRepository repository,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<AddClinicWidgetDomainCommand, Result<ClinicWidgetDomainDto>>
{
    public async Task<Result<ClinicWidgetDomainDto>> Handle(
        AddClinicWidgetDomainCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalized = ClinicWidgetDomain.Create(request.ClinicId, request.Domain);
            if (await repository.ExistsActiveAsync(request.ClinicId, normalized.Domain, cancellationToken))
                return Result.Failure<ClinicWidgetDomainDto>("El dominio ya está autorizado.");

            var domains = await repository.GetForClinicAsync(request.ClinicId, cancellationToken);
            if (domains.Count(domain => domain.IsActive) >= 5)
                return Result.Failure<ClinicWidgetDomainDto>("La clínica alcanzó el máximo de 5 dominios autorizados.");

            await repository.AddAsync(normalized, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (auditLog is not null && request.ActorId != Guid.Empty)
            {
                await auditLog.AddAsync(AuditLogEntry.Create(
                    request.ActorId, AuditAction.ClinicWidgetDomainAuthorized,
                    "ClinicWidgetDomain", normalized.Id.ToString(), normalized.Domain), cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return Result.Success(ClinicWidgetDomainDto.FromDomain(normalized));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<ClinicWidgetDomainDto>(exception.Message);
        }
    }
}

public sealed record RemoveClinicWidgetDomainCommand(Guid DomainId, Guid ActorId = default)
    : IRequest<Result<ClinicWidgetDomainDto>>;

public sealed class RemoveClinicWidgetDomainCommandHandler(
    IClinicWidgetDomainRepository repository,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<RemoveClinicWidgetDomainCommand, Result<ClinicWidgetDomainDto>>
{
    public async Task<Result<ClinicWidgetDomainDto>> Handle(
        RemoveClinicWidgetDomainCommand request,
        CancellationToken cancellationToken)
    {
        var domain = await repository.GetByIdAsync(request.DomainId, cancellationToken);
        if (domain is null)
            return Result.Failure<ClinicWidgetDomainDto>("Dominio no encontrado.");

        domain.Deactivate();
        repository.Update(domain);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (auditLog is not null && request.ActorId != Guid.Empty)
        {
            await auditLog.AddAsync(AuditLogEntry.Create(
                request.ActorId, AuditAction.ClinicWidgetDomainRevoked,
                "ClinicWidgetDomain", domain.Id.ToString(), domain.Domain), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Result.Success(ClinicWidgetDomainDto.FromDomain(domain));
    }
}
