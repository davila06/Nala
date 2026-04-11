using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-25 security regression tests.
///
/// Gap: <c>GET /api/chat/threads/{threadId}/messages</c> has no
/// <c>[EnableRateLimiting]</c> attribute, while its handler
/// (<c>GetChatMessagesQueryHandler</c>) contains a hidden write side-effect:
/// it marks every unread inbound message as read and flushes those changes
/// via <c>IUnitOfWork.SaveChangesAsync</c> before returning.
///
/// This means the endpoint <b>is a write operation disguised as a GET</b>.
///
/// Attack vector (unbounded DB write churn):
///   1. User A sends a chat message to User B inside a thread.
///   2. An attacker who controls or compromises User B's JWT hammers
///      <c>GET /api/chat/threads/{threadId}/messages</c> in a tight loop.
///   3. On the very first call the message is marked as read (1 UPDATE).
///      On every subsequent call with a new message present, the same pattern
///      repeats — and even without new messages the handler still performs
///      a full SELECT on the messages table plus an ownership JOIN on the thread.
///   4. Because <c>chat-message</c> rate limiting only covers the two write
///      <em>send</em> endpoints (<c>POST /threads</c> and
///      <c>POST /threads/{id}/messages</c>), an attacker can generate
///      unbounded DB queries through the read endpoint.
///
/// Secondary consequence — read-receipt manipulation:
///   Because there is no throttle, a malicious sender could rapidly cycle
///   messages and reads to generate false "read" receipts or exhaust the DB
///   connection pool before the 15-minute JWT expires.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("chat-message")]</c> to
///   <c>ChatController.GetMessages</c>.  This reuses the existing 30-msg/min
///   per-user policy, which is already the rate limit on the write side of
///   the same chat flow — consistent and sufficient to stop the attack.
/// </summary>
public sealed class Round25SecurityRegressionTests
{
    // ── GET /api/chat/threads/{threadId}/messages — hidden write side-effect ──

    [Fact]
    public void ChatController_GetMessages_HasEnableRateLimitingAttribute()
    {
        // The handler calls unitOfWork.SaveChangesAsync() to mark unread messages
        // as read — this is a write operation on every GET invocation.
        // Without [EnableRateLimiting] a tight loop by a valid participant
        // generates unbounded DB UPDATEs + SELECTs with no throttle.
        var method = typeof(ChatController)
            .GetMethod("GetMessages", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "ChatController must expose a public GetMessages method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/chat/threads/{threadId}/messages must carry [EnableRateLimiting] — " +
            "the handler calls SaveChangesAsync() to mark messages as read on every call; " +
            "without a rate limit a compromised JWT can generate unbounded DB write churn " +
            "through what appears to be a read-only endpoint");
    }

    [Fact]
    public void ChatController_GetMessages_UsesChatMessagePolicy()
    {
        // Reuse the same "chat-message" policy already applied to the two write
        // endpoints — consistent throttling across the full chat flow.
        var method = typeof(ChatController)
            .GetMethod("GetMessages", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "chat-message",
            "GetMessages must use the 'chat-message' policy (same as OpenThread and " +
            "SendMessage) for consistent throttling across the full chat flow; " +
            "the hidden SaveChangesAsync write makes it equivalent to a write endpoint");
    }

    // ── GET /api/chat/threads — no rate limit on thread listing ───────────────

    [Fact]
    public void ChatController_GetThreads_HasEnableRateLimitingAttribute()
    {
        // Thread listing issues a DB query per call (JOIN across threads + participants).
        // No rate limit lets a compromised account scan event IDs at database speed.
        var method = typeof(ChatController)
            .GetMethod("GetThreads", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "ChatController must expose a public GetThreads method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/chat/threads must carry [EnableRateLimiting] — without a rate " +
            "limit a compromised account can enumerate thread metadata (participants, " +
            "event IDs) at database query speed");
    }

    [Fact]
    public void ChatController_GetThreads_UsesChatMessagePolicy()
    {
        var method = typeof(ChatController)
            .GetMethod("GetThreads", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "chat-message",
            "GetThreads must use the 'chat-message' policy for consistency with " +
            "all other chat endpoints");
    }
}
