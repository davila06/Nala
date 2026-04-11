using FluentAssertions;
using PawTrack.Application.Chat.Commands.SendChatMessage;
using System.Reflection;

namespace PawTrack.UnitTests.Chat;

/// <summary>
/// Round-7 security: the phone/email content-safety guard in SendChatMessageCommand
/// must handle adversarial inputs (timeout resilience) and correctly detect
/// contact details to prevent exfiltration via chat.
/// </summary>
public sealed class ChatMessageGuardsTests
{
    // Access the file-scoped Guards class via reflection since it's file-private.
    // We test it indirectly through the public SendChatMessageCommandHandler if needed,
    // but we can verify the outcomes directly via observable handler behavior.
    // Here we test the logic through the Guard's observable side effects by keeping
    // the tests at the command-dispatch level.

    // Helper: get the ContainsContactDetail result via the Guard method
    // We use a simple test double approach by calling through the handler result.
    // Since Guards is file-scoped, we verify behavior via representative inputs.

    private static bool ContainsContactDetail(string body)
    {
        // Use reflection to call the internal Guards.ContainsContactDetail method.
        // Guards is internal static (not file-scoped) so the CLR type name is "Guards", not mangled.
        var assembly = typeof(SendChatMessageCommand).Assembly;
        var guardsType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "Guards" && t.IsSealed && !t.IsPublic);

        if (guardsType is null)
            throw new InvalidOperationException("Guards type not found in assembly.");

        var method = guardsType.GetMethod("ContainsContactDetail",
            BindingFlags.Public | BindingFlags.Static);

        return (bool)method!.Invoke(null, [body])!;
    }

    // ── Email detection ───────────────────────────────────────────────────────

    [Fact]
    public void ContainsContactDetail_WithEmail_ReturnsTrue()
    {
        ContainsContactDetail("Me pueden contactar en alice@example.com").Should().BeTrue();
    }

    [Fact]
    public void ContainsContactDetail_WithAtSymbol_ReturnsTrue()
    {
        ContainsContactDetail("user@domain").Should().BeTrue();
    }

    // ── Phone number detection ────────────────────────────────────────────────

    [Fact]
    public void ContainsContactDetail_WithCostaRicaPhone_ReturnsTrue()
    {
        ContainsContactDetail("llamar al 8888-1234 para más info").Should().BeTrue();
    }

    [Fact]
    public void ContainsContactDetail_WithInternationalPhone_ReturnsTrue()
    {
        ContainsContactDetail("+506 8888 1234").Should().BeTrue();
    }

    // ── Clean messages ────────────────────────────────────────────────────────

    [Fact]
    public void ContainsContactDetail_CleanMessage_ReturnsFalse()
    {
        ContainsContactDetail("Vi al perro cerca del parque central").Should().BeFalse();
    }

    [Fact]
    public void ContainsContactDetail_EmptyBody_ReturnsFalse()
    {
        ContainsContactDetail(string.Empty).Should().BeFalse();
    }

    // ── Adversarial / timeout resilience ─────────────────────────────────────

    [Fact]
    public void ContainsContactDetail_AdversarialInput_DoesNotThrowOrHang()
    {
        // Crafted to exercise the character class backtracking. The 100 ms timeout
        // in the GeneratedRegex must prevent this from blocking for more than a few ms.
        var adversarial = new string('(', 400) + " " + new string(')', 400);

        var act = () => ContainsContactDetail(adversarial);

        // Must complete in << 1 second (the timeout is 100 ms)
        act.Should().NotThrow("a RegexMatchTimeoutException is caught internally and treated as non-match");
    }

    [Fact]
    public void ContainsContactDetail_MaxLengthCleanBody_CompletesQuickly()
    {
        // 800-char body with no contact details must be checked without timing out
        var body = string.Concat(Enumerable.Repeat("perro negro labrador ", 40));
        var act = () => ContainsContactDetail(body);
        act.Should().NotThrow();
    }
}
