using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicCrm;

public sealed record ClinicCrmPreferenceDto(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, string Channel, string Purpose, bool IsOptedIn, string ConsentSource, DateTimeOffset UpdatedAt)
{
    public static ClinicCrmPreferenceDto FromReadModel(ClinicCrmPreferenceReadModel model) => new(model.Id, model.PetId, model.PetName, model.OwnerUserId, model.OwnerName, model.Channel.ToString(), model.Purpose.ToString(), model.IsOptedIn, model.ConsentSource, model.UpdatedAt);
}

public sealed record ClinicCrmActivityDto(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, string Channel, string Purpose, string Direction, string Status, string Subject, string Body, DateTimeOffset CreatedAt)
{
    public static ClinicCrmActivityDto FromReadModel(ClinicCrmActivityReadModel model) => new(model.Id, model.PetId, model.PetName, model.OwnerUserId, model.OwnerName, model.Channel.ToString(), model.Purpose.ToString(), model.Direction.ToString(), model.Status.ToString(), model.Subject, model.Body, model.CreatedAt);
}

public sealed record ClinicCrmTaskDto(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, string Type, string Status, DateOnly DueDate, string Title, string? Notes)
{
    public static ClinicCrmTaskDto FromReadModel(ClinicCrmTaskReadModel model) => new(model.Id, model.PetId, model.PetName, model.OwnerUserId, model.OwnerName, model.Type.ToString(), model.Status.ToString(), model.DueDate, model.Title, model.Notes);
    public static ClinicCrmTaskDto FromDomain(ClinicCrmTask task, string petName, string ownerName) => new(task.Id, task.PetId, petName, task.OwnerUserId, ownerName, task.Type.ToString(), task.Status.ToString(), task.DueDate, task.Title, task.Notes);
}

public sealed record ClinicCrmSegmentDto(string Key, string Label, int Count, IReadOnlyList<Guid> PetIds);
public sealed record ClinicCrmDashboardDto(IReadOnlyList<ClinicCrmPreferenceDto> Preferences, IReadOnlyList<ClinicCrmActivityDto> RecentActivities, IReadOnlyList<ClinicCrmTaskDto> OpenTasks, IReadOnlyList<ClinicCrmSegmentDto> Segments);

public sealed record UpsertClinicCommunicationPreferenceCommand(Guid ClinicId, Guid ClinicUserId, Guid PetId, ClinicCommunicationChannel Channel, ClinicCommunicationPurpose Purpose, bool IsOptedIn, string ConsentSource) : IRequest<Result<bool>>;

public sealed record SetOwnerClinicCommunicationPreferenceCommand(Guid ClinicId, Guid OwnerUserId, Guid PetId, ClinicCommunicationChannel Channel, ClinicCommunicationPurpose Purpose, bool IsOptedIn) : IRequest<Result<bool>>;

public sealed record GetOwnerClinicCommunicationPreferencesQuery(Guid OwnerUserId, Guid PetId) : IRequest<Result<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>>>;

public sealed class GetOwnerClinicCommunicationPreferencesQueryHandler(IPetRepository petRepository, IClinicCrmRepository crmRepository)
    : IRequestHandler<GetOwnerClinicCommunicationPreferencesQuery, Result<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>>>
{
    public async Task<Result<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>>> Handle(GetOwnerClinicCommunicationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.OwnerUserId)
            return Result.Failure<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>>("Mascota no encontrada.");
        return Result.Success(await crmRepository.GetOwnerPreferencesForPetAsync(request.PetId, cancellationToken));
    }
}

public sealed class SetOwnerClinicCommunicationPreferenceCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicCrmRepository crmRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetOwnerClinicCommunicationPreferenceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SetOwnerClinicCommunicationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.OwnerUserId)
            return Result.Failure<bool>("Mascota no encontrada.");
        if (await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken) is null
            || !await crmRepository.HasClinicPatientRelationshipAsync(request.ClinicId, request.PetId, cancellationToken))
            return Result.Failure<bool>("No existe relación clínica con esta mascota.");
        var preference = await crmRepository.GetPreferenceAsync(request.ClinicId, request.PetId, request.Channel, request.Purpose, cancellationToken);
        if (preference is null)
            await crmRepository.AddPreferenceAsync(ClinicClientCommunicationPreference.Create(request.ClinicId, request.PetId, pet.OwnerId, request.Channel, request.Purpose, request.IsOptedIn, "Portal del tutor", request.OwnerUserId), cancellationToken);
        else
        {
            preference.Update(request.IsOptedIn, "Portal del tutor", request.OwnerUserId);
            crmRepository.UpdatePreference(preference);
        }
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.OwnerUserId, AuditAction.ClinicCrmPreferenceUpdated, "ClinicCommunicationPreference", request.PetId.ToString(), $"{request.Channel}:{request.Purpose}:{request.IsOptedIn}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

public sealed class UpsertClinicCommunicationPreferenceCommandValidator : AbstractValidator<UpsertClinicCommunicationPreferenceCommand>
{
    public UpsertClinicCommunicationPreferenceCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ConsentSource).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpsertClinicCommunicationPreferenceCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicCrmRepository crmRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertClinicCommunicationPreferenceCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpsertClinicCommunicationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<bool>("Acceso denegado.");
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<bool>("Mascota no encontrada.");
        if (request.IsOptedIn)
            return Result.Failure<bool>("Solo el tutor puede otorgar consentimiento de comunicación.");
        if (!await crmRepository.HasClinicPatientRelationshipAsync(request.ClinicId, request.PetId, cancellationToken))
            return Result.Failure<bool>("Paciente no vinculado a esta clínica.");
        var preference = await crmRepository.GetPreferenceAsync(request.ClinicId, request.PetId, request.Channel, request.Purpose, cancellationToken);
        if (preference is null)
            await crmRepository.AddPreferenceAsync(ClinicClientCommunicationPreference.Create(request.ClinicId, request.PetId, pet.OwnerId, request.Channel, request.Purpose, request.IsOptedIn, request.ConsentSource, request.ClinicUserId), cancellationToken);
        else
        {
            preference.Update(request.IsOptedIn, request.ConsentSource, request.ClinicUserId);
            crmRepository.UpdatePreference(preference);
        }
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmPreferenceUpdated, "ClinicCommunicationPreference", request.PetId.ToString(), $"{request.Channel}:{request.Purpose}:{request.IsOptedIn}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

public sealed record LogClinicCommunicationActivityCommand(Guid ClinicId, Guid ClinicUserId, Guid PetId, ClinicCommunicationChannel Channel, ClinicCommunicationPurpose Purpose, ClinicCommunicationDirection Direction, ClinicCommunicationStatus Status, string Subject, string Body, string? ProviderMessageId) : IRequest<Result<bool>>;

public sealed class LogClinicCommunicationActivityCommandValidator : AbstractValidator<LogClinicCommunicationActivityCommand>
{
    public LogClinicCommunicationActivityCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class LogClinicCommunicationActivityCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicCrmRepository crmRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogClinicCommunicationActivityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(LogClinicCommunicationActivityCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<bool>("Acceso denegado.");
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<bool>("Mascota no encontrada.");
        if (!await crmRepository.HasClinicPatientRelationshipAsync(request.ClinicId, request.PetId, cancellationToken))
            return Result.Failure<bool>("Paciente no vinculado a esta clínica.");
        if (request.Status != ClinicCommunicationStatus.LoggedExternally || request.ProviderMessageId is not null)
            return Result.Failure<bool>("Solo se puede registrar contacto externo, sin afirmar entrega por proveedor.");
        if (request.Direction == ClinicCommunicationDirection.Outbound)
        {
            var preference = await crmRepository.GetPreferenceAsync(request.ClinicId, request.PetId, request.Channel, request.Purpose, cancellationToken);
            if (preference?.IsOptedIn != true)
                return Result.Failure<bool>("El tutor no ha autorizado este canal y propósito.");
        }
        var activity = ClinicClientCommunicationActivity.Log(request.ClinicId, request.PetId, pet.OwnerId, request.Channel, request.Purpose, request.Direction, request.Status, request.Subject, request.Body, request.ProviderMessageId, request.ClinicUserId);
        await crmRepository.AddActivityAsync(activity, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmActivityLogged, "ClinicCommunicationActivity", activity.Id.ToString(), request.Subject), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

public sealed record CreateClinicCrmTaskCommand(Guid ClinicId, Guid ClinicUserId, Guid PetId, ClinicCrmTaskType Type, DateOnly DueDate, string Title, string? Notes) : IRequest<Result<Guid>>;

public sealed class CreateClinicCrmTaskCommandValidator : AbstractValidator<CreateClinicCrmTaskCommand>
{
    public CreateClinicCrmTaskCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateClinicCrmTaskCommandHandler(IClinicRepository clinicRepository, IPetRepository petRepository, IClinicCrmRepository crmRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateClinicCrmTaskCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateClinicCrmTaskCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<Guid>("Acceso denegado.");
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<Guid>("Mascota no encontrada.");
        if (!await crmRepository.HasClinicPatientRelationshipAsync(request.ClinicId, request.PetId, cancellationToken))
            return Result.Failure<Guid>("Paciente no vinculado a esta clínica.");
        var task = ClinicCrmTask.Create(request.ClinicId, request.PetId, pet.OwnerId, request.Type, request.DueDate, request.Title, request.Notes, request.ClinicUserId);
        await crmRepository.AddTaskAsync(task, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmTaskCreated, "ClinicCrmTask", task.Id.ToString(), task.Title), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(task.Id);
    }
}

public sealed record CompleteClinicCrmTaskCommand(Guid ClinicId, Guid ClinicUserId, Guid TaskId) : IRequest<Result<bool>>;

public sealed class CompleteClinicCrmTaskCommandHandler(IClinicRepository clinicRepository, IClinicCrmRepository crmRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteClinicCrmTaskCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CompleteClinicCrmTaskCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<bool>("Acceso denegado.");
        var task = await crmRepository.GetTaskByIdAsync(request.TaskId, cancellationToken);
        if (task is null || task.ClinicId != request.ClinicId)
            return Result.Failure<bool>("Tarea CRM no encontrada.");
        try { task.Complete(request.ClinicUserId); }
        catch (InvalidOperationException ex) { return Result.Failure<bool>(ex.Message); }
        crmRepository.UpdateTask(task);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmTaskCompleted, "ClinicCrmTask", task.Id.ToString(), task.Title), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

public sealed record GetClinicCrmDashboardQuery(Guid ClinicId, Guid ClinicUserId, DateOnly Today) : IRequest<Result<ClinicCrmDashboardDto>>;

public sealed class GetClinicCrmDashboardQueryHandler(IClinicRepository clinicRepository, IClinicCrmRepository crmRepository)
    : IRequestHandler<GetClinicCrmDashboardQuery, Result<ClinicCrmDashboardDto>>
{
    public async Task<Result<ClinicCrmDashboardDto>> Handle(GetClinicCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicCrmDashboardDto>("Acceso denegado.");
        var dashboard = await crmRepository.GetDashboardAsync(request.ClinicId, request.Today, cancellationToken);
        return Result.Success(new ClinicCrmDashboardDto(
            dashboard.Preferences.Select(ClinicCrmPreferenceDto.FromReadModel).ToList(),
            dashboard.RecentActivities.Select(ClinicCrmActivityDto.FromReadModel).ToList(),
            dashboard.OpenTasks.Select(ClinicCrmTaskDto.FromReadModel).ToList(),
            dashboard.Segments.Select(segment => new ClinicCrmSegmentDto(segment.Key, segment.Label, segment.Count, segment.PetIds)).ToList()));
    }
}
