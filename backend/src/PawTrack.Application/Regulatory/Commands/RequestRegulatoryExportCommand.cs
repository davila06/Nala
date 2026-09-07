using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Regulatory;
using PawTrack.Domain.Common;
using Microsoft.ApplicationInsights;

namespace PawTrack.Application.Regulatory.Commands;

public sealed record RequestRegulatoryExportCommand(
    Guid RequestedByUserId,
    ReportType ReportType,
    ExportScope Scope,
    ExportFormat Format,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Canton,
    Guid? OrganizationId,
    string IdempotencyKey) : IRequest<Result<RegulatoryExportDto>>;

public sealed class RequestRegulatoryExportCommandValidator : AbstractValidator<RequestRegulatoryExportCommand>
{
    public RequestRegulatoryExportCommandValidator()
    {
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart);
        RuleFor(x => x.PeriodEnd.DayNumber - x.PeriodStart.DayNumber)
            .LessThanOrEqualTo(366)
            .WithMessage("El periodo máximo del reporte es de 366 días.");
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Canton).MaximumLength(80);
    }
}

public sealed class RequestRegulatoryExportCommandHandler(
    IReportDefinitionRepository definitionRepository,
    IRegulatoryExportRepository exportRepository,
    IAuditLogRepository auditLogRepository,
    IReportAuthorizationService authorizationService,
    TelemetryClient telemetryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestRegulatoryExportCommand, Result<RegulatoryExportDto>>
{
    public async Task<Result<RegulatoryExportDto>> Handle(RequestRegulatoryExportCommand request, CancellationToken ct)
    {
        var existing = await exportRepository.GetByIdempotencyKeyAsync(request.RequestedByUserId, request.IdempotencyKey.Trim(), ct);
        if (existing is not null) return Result.Success(existing.ToDto());

        var definition = await definitionRepository.GetActiveAsync(request.ReportType, request.Scope, ct);
        if (definition is null)
            return Result.Failure<RegulatoryExportDto>("No existe una definición activa para este reporte y alcance.");

        if (request.PeriodEnd > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            return Result.Failure<RegulatoryExportDto>("El periodo no puede estar en el futuro.");

        var authorization = await authorizationService.AuthorizeAsync(
            request.RequestedByUserId,
            request.Scope,
            request.Canton,
            request.OrganizationId,
            request.ReportType,
            ct);
        if (!authorization.IsAuthorized)
            return Result.Failure<RegulatoryExportDto>(authorization.Error!);

        var export = RegulatoryExport.Request(
            request.ReportType,
            request.Scope,
            request.Format,
            definition.SchemaVersion,
            request.RequestedByUserId,
            request.OrganizationId,
            request.Canton,
            request.PeriodStart,
            request.PeriodEnd,
            "America/Costa_Rica",
            request.IdempotencyKey);

        await exportRepository.AddAsync(export, ct);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestedByUserId,
            AuditAction.RegulatoryExportRequested,
            "RegulatoryExport",
            export.Id.ToString(),
            $"{export.ReportType}:{export.Scope}:{export.Format}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        telemetryClient.TrackEvent("RegulatoryExport.Requested", new Dictionary<string, string>
        {
            ["reportType"] = export.ReportType.ToString(),
            ["scope"] = export.Scope.ToString(),
            ["format"] = export.Format.ToString(),
        });
        return Result.Success(export.ToDto());
    }
}
