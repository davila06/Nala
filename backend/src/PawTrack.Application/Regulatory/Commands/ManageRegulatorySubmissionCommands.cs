using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Regulatory.Commands;

public sealed record CancelRegulatorySubmissionCommand(Guid SubmissionId, Guid RequestedByUserId) : IRequest<Result<bool>>;
public sealed record RetryRegulatorySubmissionCommand(Guid SubmissionId, Guid RequestedByUserId) : IRequest<Result<bool>>;

public sealed class CancelRegulatorySubmissionCommandHandler(
    IRegulatorySubmissionRepository submissionRepository,
    IRegulatoryExportRepository exportRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelRegulatorySubmissionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelRegulatorySubmissionCommand request, CancellationToken ct)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return Result.Failure<bool>("Submission no encontrada.");
        var export = await exportRepository.GetByIdAsync(submission.ExportId, ct);
        if (export is null || export.RequestedByUserId != request.RequestedByUserId)
            return Result.Failure<bool>("Submission no encontrada.");
        var result = submission.Cancel();
        if (result.IsFailure) return result;
        submissionRepository.Update(submission);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed class RetryRegulatorySubmissionCommandHandler(
    IRegulatorySubmissionRepository submissionRepository,
    IRegulatoryExportRepository exportRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RetryRegulatorySubmissionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RetryRegulatorySubmissionCommand request, CancellationToken ct)
    {
        var submission = await submissionRepository.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return Result.Failure<bool>("Submission no encontrada.");
        var export = await exportRepository.GetByIdAsync(submission.ExportId, ct);
        if (export is null || export.RequestedByUserId != request.RequestedByUserId)
            return Result.Failure<bool>("Submission no encontrada.");
        var result = submission.Retry();
        if (result.IsFailure) return result;
        submissionRepository.Update(submission);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}
