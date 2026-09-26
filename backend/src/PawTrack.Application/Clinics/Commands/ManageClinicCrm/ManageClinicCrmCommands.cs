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

public sealed record ClinicCrmTaskDto(
    Guid Id,
    Guid? PetId,
    string? PetName,
    Guid? OwnerUserId,
    string? OwnerName,
    string Type,
    string AssignedRole,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string Priority,
    string Status,
    DateOnly DueDate,
    string Title,
    string? Notes)
{
    public static ClinicCrmTaskDto FromReadModel(ClinicCrmTaskReadModel model) => new(
        model.Id, model.PetId, model.PetName, model.OwnerUserId, model.OwnerName, model.Type.ToString(),
        model.AssignedRole.ToString(), model.AssignedToUserId, model.AssignedToName, model.Priority.ToString(), model.Status.ToString(),
        model.DueDate, model.Title, model.Notes);

    public static ClinicCrmTaskDto FromDomain(ClinicCrmTask task, string? petName, string? ownerName, string? assignedToName = null) => new(
        task.Id, task.PetId, petName, task.OwnerUserId, ownerName, task.Type.ToString(), task.AssignedRole.ToString(),
        task.AssignedToUserId, assignedToName, task.Priority.ToString(), task.Status.ToString(), task.DueDate, task.Title, task.Notes);
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

public sealed record CreateClinicCrmTaskCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid? PetId,
    ClinicCrmTaskType Type,
    DateOnly DueDate,
    string Title,
    string? Notes,
    Guid IdempotencyKey,
    ClinicCrmTaskPriority Priority = ClinicCrmTaskPriority.Normal,
    ClinicInternalTaskRole? AssignedRole = null,
    Guid? AssignedToUserId = null) : IRequest<Result<Guid>>;

public sealed class CreateClinicCrmTaskCommandValidator : AbstractValidator<CreateClinicCrmTaskCommand>
{
    public CreateClinicCrmTaskCommandValidator()
    {
        RuleFor(x => x.PetId).Must(petId => !petId.HasValue || petId.Value != Guid.Empty);
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateClinicCrmTaskCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicStaffAccessRepository staffAccess,
    IClinicFinanceAccessRepository financeAccess,
    IClinicCrmRepository crmRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateClinicCrmTaskCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateClinicCrmTaskCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<Guid>("Acceso denegado.");
        var isOwner = clinic.UserId == request.ClinicUserId;
        var staffMembership = isOwner ? null : await staffAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
        var financeMembership = isOwner ? null : await financeAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
        var actorRole = isOwner ? ClinicInternalTaskRole.Manager : staffMembership?.InternalTaskRole ?? financeMembership?.InternalTaskRole;
        var isManager = financeMembership?.InternalTaskRole == ClinicInternalTaskRole.Manager;
        var canWorkType = staffMembership?.CanWorkCrmTask(request.Type) == true
            || financeMembership?.CanWorkCrmTask(request.Type) == true;
        if (!isOwner && !canWorkType)
            return Result.Failure<Guid>("Acceso denegado.");

        var assignedRole = request.AssignedRole ?? (isOwner || isManager
            ? ClinicCrmTask.DefaultAssignedRoleFor(request.Type)
            : actorRole);
        if (!assignedRole.HasValue || !Enum.IsDefined(assignedRole.Value))
            return Result.Failure<Guid>("Rol responsable inválido.");
        if (assignedRole.Value != ClinicCrmTask.DefaultAssignedRoleFor(request.Type))
            return Result.Failure<Guid>("El tipo de tarea no corresponde al rol responsable.");
        if (!isOwner && !isManager && assignedRole != actorRole)
            return Result.Failure<Guid>("No puedes asignar tareas a otro rol.");

        var assignedUserId = request.AssignedToUserId ?? (assignedRole == actorRole ? request.ClinicUserId : null);
        if (assignedUserId.HasValue && !(isOwner && assignedUserId == clinic.UserId))
        {
            var targetStaff = await staffAccess.GetAsync(request.ClinicId, assignedUserId.Value, cancellationToken);
            var targetFinance = await financeAccess.GetAsync(request.ClinicId, assignedUserId.Value, cancellationToken);
            if (targetStaff?.InternalTaskRole != assignedRole && targetFinance?.InternalTaskRole != assignedRole)
                return Result.Failure<Guid>("La persona responsable no pertenece al rol seleccionado.");
        }

        Guid? ownerUserId = null;
        if (request.PetId.HasValue)
        {
            var pet = await petRepository.GetByIdAsync(request.PetId.Value, cancellationToken);
            if (pet is null)
                return Result.Failure<Guid>("Mascota no encontrada.");
            if (!await crmRepository.HasClinicPatientRelationshipAsync(request.ClinicId, request.PetId.Value, cancellationToken))
                return Result.Failure<Guid>("Paciente no vinculado a esta clínica.");
            ownerUserId = pet.OwnerId;
        }

        var existing = await crmRepository.GetTaskByIdempotencyKeyAsync(request.ClinicId, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.MatchesRequest(request.PetId, ownerUserId, request.Type, assignedRole.Value,
                    assignedUserId, request.Priority, request.DueDate, request.Title, request.Notes)
                ? Result.Success(existing.Id)
                : Result.Failure<Guid>("IDEMPOTENCY_CONFLICT");
        }

        var task = ClinicCrmTask.Create(request.ClinicId, request.PetId, ownerUserId, request.Type, request.DueDate,
            request.Title, request.Notes, request.ClinicUserId, assignedRole, request.Priority, assignedUserId, request.IdempotencyKey);
        await crmRepository.AddTaskAsync(task, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmTaskCreated, "ClinicCrmTask", task.Id.ToString(), task.Title), cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            crmRepository.DetachTask(task);
            var winner = await crmRepository.GetTaskByIdempotencyKeyAsync(request.ClinicId, request.IdempotencyKey, cancellationToken);
            if (winner is null) throw;
            if (!winner.MatchesRequest(request.PetId, ownerUserId, request.Type, assignedRole.Value,
                    assignedUserId, request.Priority, request.DueDate, request.Title, request.Notes))
                return Result.Failure<Guid>("IDEMPOTENCY_CONFLICT");
            return Result.Success(winner.Id);
        }
        return Result.Success(task.Id);
    }
}

public sealed record CompleteClinicCrmTaskCommand(Guid ClinicId, Guid ClinicUserId, Guid TaskId) : IRequest<Result<bool>>;

public sealed class CompleteClinicCrmTaskCommandHandler(
    IClinicRepository clinicRepository,
    IClinicStaffAccessRepository staffAccess,
    IClinicFinanceAccessRepository financeAccess,
    IClinicCrmRepository crmRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteClinicCrmTaskCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CompleteClinicCrmTaskCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<bool>("Acceso denegado.");
        var task = await crmRepository.GetTaskByIdAsync(request.TaskId, cancellationToken);
        if (task is null || task.ClinicId != request.ClinicId)
            return Result.Failure<bool>("Tarea CRM no encontrada.");
        if (clinic.UserId != request.ClinicUserId)
        {
            var staffMembership = await staffAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
            var financeMembership = await financeAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
            var isManager = financeMembership?.InternalTaskRole == ClinicInternalTaskRole.Manager;
            var assignedToActor = !task.AssignedToUserId.HasValue || task.AssignedToUserId == request.ClinicUserId;
            var staffCanComplete = staffMembership?.InternalTaskRole == task.AssignedRole
                && staffMembership.CanWorkCrmTask(task.Type) && assignedToActor;
            var financeCanComplete = financeMembership?.InternalTaskRole == task.AssignedRole
                && financeMembership.CanWorkCrmTask(task.Type) && assignedToActor;
            if (!isManager && !staffCanComplete && !financeCanComplete)
                return Result.Failure<bool>("Acceso denegado.");
        }
        try { task.Complete(request.ClinicUserId); }
        catch (InvalidOperationException ex) { return Result.Failure<bool>(ex.Message); }
        crmRepository.UpdateTask(task);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmTaskCompleted, "ClinicCrmTask", task.Id.ToString(), task.Title), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

public sealed record GetClinicCrmDashboardQuery(Guid ClinicId, Guid ClinicUserId, DateOnly Today) : IRequest<Result<ClinicCrmDashboardDto>>;

public sealed class GetClinicCrmDashboardQueryHandler(
    IClinicRepository clinicRepository,
    IClinicStaffAccessRepository staffAccess,
    IClinicFinanceAccessRepository financeAccess,
    IClinicCrmRepository crmRepository)
    : IRequestHandler<GetClinicCrmDashboardQuery, Result<ClinicCrmDashboardDto>>
{
    public async Task<Result<ClinicCrmDashboardDto>> Handle(GetClinicCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicCrmDashboardDto>("Acceso denegado.");
        var isOwner = clinic.UserId == request.ClinicUserId;
        var staffMembership = isOwner ? null : await staffAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
        var financeMembership = isOwner ? null : await financeAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
        var isManager = financeMembership?.InternalTaskRole == ClinicInternalTaskRole.Manager;
        var allowedRoles = new HashSet<ClinicInternalTaskRole>();
        if (staffMembership?.InternalTaskRole is { } staffRole)
            allowedRoles.Add(staffRole);
        if (financeMembership?.InternalTaskRole is { } financeRole)
            allowedRoles.Add(financeRole);
        if (isManager)
            allowedRoles.UnionWith(Enum.GetValues<ClinicInternalTaskRole>());
        if (!isOwner && allowedRoles.Count == 0)
            return Result.Failure<ClinicCrmDashboardDto>("Acceso denegado.");
        var dashboard = await crmRepository.GetDashboardAsync(
            request.ClinicId,
            request.Today,
            isOwner || isManager ? null : allowedRoles.ToArray(),
            null,
            isOwner,
            isOwner || isManager ? null : request.ClinicUserId,
            cancellationToken);
        var tasks = dashboard.OpenTasks;
        return Result.Success(new ClinicCrmDashboardDto(
            isOwner ? dashboard.Preferences.Select(ClinicCrmPreferenceDto.FromReadModel).ToList() : [],
            isOwner ? dashboard.RecentActivities.Select(ClinicCrmActivityDto.FromReadModel).ToList() : [],
            tasks.Select(ClinicCrmTaskDto.FromReadModel).ToList(),
            isOwner ? dashboard.Segments.Select(segment => new ClinicCrmSegmentDto(segment.Key, segment.Label, segment.Count, segment.PetIds)).ToList() : []));
    }
}
