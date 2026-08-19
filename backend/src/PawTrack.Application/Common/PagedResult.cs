namespace PawTrack.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>Optional: set by handlers that cheaply know the unread count alongside the main query.</summary>
    public int? UnreadCount { get; init; }
}
