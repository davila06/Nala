namespace PawTrack.Application.Common;

/// <summary>
/// Cursor-based page result. Cursor is the opaque ID of the last returned item.
/// Client passes it back as <c>?cursor=xxx</c> to get the next page.
/// Avoids OFFSET performance degradation on large tables (O(1) vs O(offset)).
/// </summary>
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    /// <summary>Opaque cursor to pass on the next request. Null if this is the last page.</summary>
    string? NextCursor,
    bool HasMore)
{
    /// <summary>Creates a result from an ordered list where <paramref name="idSelector"/> yields the cursor key.</summary>
    public static CursorPagedResult<T> From(
        IReadOnlyList<T> items,
        int pageSize,
        Func<T, Guid> idSelector)
    {
        var hasMore = items.Count == pageSize;
        var nextCursor = hasMore ? idSelector(items[^1]).ToString() : null;
        return new CursorPagedResult<T>(items, nextCursor, hasMore);
    }
}
