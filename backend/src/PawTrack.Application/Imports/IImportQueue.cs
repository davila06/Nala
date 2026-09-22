namespace PawTrack.Application.Imports;

public sealed record ImportWorkItem(
    Guid TenantId,
    string ResourceType,
    string Format,
    string FileHash,
    string IdempotencyKey,
    byte[] Payload);

public interface IImportQueue
{
    ValueTask<bool> EnqueueAsync(ImportWorkItem item, CancellationToken cancellationToken = default);
}
