using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Safety;
using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Application.Safety.Commands.ReportFraud;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record FraudReportResultDto(
    string             Message,
    FraudSuspicionLevel SuspicionLevel);

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Records a fraud / scam attempt report.  Available to both authenticated users and
/// anonymous reporters (rate-limited externally at the API layer).
/// After persisting, the handler runs a rolling-window pattern check and returns
/// the computed <see cref="FraudSuspicionLevel"/> so the UI can give appropriate feedback.
/// </summary>
public sealed record ReportFraudCommand(
    Guid?        ReporterUserId,
    string       ReporterIpAddress,
    FraudContext Context,
    Guid?        RelatedEntityId,
    Guid?        TargetUserId,
    string?      Description)
    : IRequest<Result<FraudReportResultDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ReportFraudCommandHandler(
    IFraudReportRepository fraudReportRepository,
    IUnitOfWork            unitOfWork,
    ILogger<ReportFraudCommandHandler> logger)
    : IRequestHandler<ReportFraudCommand, Result<FraudReportResultDto>>
{
    private static readonly TimeSpan RollingWindow = TimeSpan.FromDays(7);

    public async Task<Result<FraudReportResultDto>> Handle(
        ReportFraudCommand command,
        CancellationToken  cancellationToken)
    {
        var ipHash = HashIp(command.ReporterIpAddress);

        // ── Anti-spam: anonymous reporters may file at most 5 reports per 7 days ─
        if (command.ReporterUserId is null)
        {
            var anonCount = await fraudReportRepository.CountRecentByIpHashAsync(
                ipHash, RollingWindow, cancellationToken);

            if (anonCount >= 5)
                return Result.Failure<FraudReportResultDto>(
                    "Has enviado demasiados reportes recientemente. Intenta de nuevo más tarde.");
        }

        // ── Compute suspicion level before persisting (so we can store it) ─────
        var recentTargetCount = command.TargetUserId.HasValue
            ? await fraudReportRepository.CountRecentByTargetUserAsync(
                command.TargetUserId.Value, RollingWindow, cancellationToken)
            : 0;

        // + 1 for the report we are about to add.
        var level = ComputeLevel(recentTargetCount + 1);

        // ── Persist ────────────────────────────────────────────────────────────
        var report = FraudReport.Create(
            command.ReporterUserId,
            ipHash,
            command.Context,
            command.RelatedEntityId,
            command.TargetUserId,
            command.Description,
            level);

        await fraudReportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (level >= FraudSuspicionLevel.High)
        {
            logger.LogWarning(
                "Fraud suspicion level {Level} reached for target user {TargetUserId} " +
                "(related entity: {RelatedEntityId}). Manual review required.",
                level,
                command.TargetUserId,
                command.RelatedEntityId);
        }

        var message = level switch
        {
            FraudSuspicionLevel.None     => "Tu reporte fue recibido. Gracias por ayudar a mantener la comunidad segura.",
            FraudSuspicionLevel.Elevated => "Tu reporte fue recibido. El equipo de PawTrack lo revisará.",
            FraudSuspicionLevel.High     => "Tu reporte fue recibido. Este caso está siendo escalado para revisión urgente.",
            FraudSuspicionLevel.Critical => "Tu reporte fue recibido. Este caso ha sido marcado como crítico y está siendo investigado.",
            _                            => "Tu reporte fue recibido.",
        };

        return Result.Success(new FraudReportResultDto(message, level));
    }

    private static string HashIp(string ipAddress)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress ?? "unknown"));
        return Convert.ToHexString(bytes);
    }

    private static FraudSuspicionLevel ComputeLevel(int count) => count switch
    {
        <= 1 => FraudSuspicionLevel.None,
        <= 3 => FraudSuspicionLevel.Elevated,
        <= 5 => FraudSuspicionLevel.High,
        _    => FraudSuspicionLevel.Critical,
    };
}
