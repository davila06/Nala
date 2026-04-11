using FluentAssertions;
using PawTrack.Domain.Bot;
using System.Text.Json;

namespace PawTrack.UnitTests.Bot.Domain;

/// <summary>
/// Round-10 security: BotSession domain hardening.
/// 1. SetPetName must cap names at 50 chars to prevent oversized input being echoed
///    in multiple WhatsApp messages and persisted to Pet.Name.
/// 2. MarkMessageProcessed must use proper JSON serialisation — not string concatenation —
///    so that a crafted wamid containing double-quote characters cannot corrupt the
///    ProcessedMessageIds JSON array stored in the database.
/// </summary>
public sealed class BotSessionSecurityTests
{
    // ── SetPetName — length cap ───────────────────────────────────────────────

    [Fact]
    public void SetPetName_ShortName_StoredAsIs()
    {
        var session = BotSession.Create("abc123hash");

        session.SetPetName("Firulais");

        session.PetName.Should().Be("Firulais");
    }

    [Fact]
    public void SetPetName_NameExactly50Chars_StoredUntruncated()
    {
        var session = BotSession.Create("abc123hash");
        var name50 = new string('A', 50);

        session.SetPetName(name50);

        session.PetName.Should().HaveLength(50);
        session.PetName.Should().Be(name50);
    }

    [Fact]
    public void SetPetName_NameLongerThan50Chars_IsTruncatedTo50()
    {
        var session = BotSession.Create("abc123hash");
        var longName = new string('A', 200);

        session.SetPetName(longName);

        session.PetName!.Length.Should().Be(50,
            "names longer than 50 chars must be truncated before storage and echo");
    }

    [Fact]
    public void SetPetName_4096CharName_IsTruncatedTo50()
    {
        // WhatsApp TextBody validator allows up to 4096 chars; the domain layer must
        // enforce a tighter pet-name limit independently.
        var session = BotSession.Create("abc123hash");
        var maxTextBody = new string('Z', 4096);

        session.SetPetName(maxTextBody);

        session.PetName!.Length.Should().Be(50);
    }

    // ── MarkMessageProcessed — JSON safety ────────────────────────────────────

    [Fact]
    public void MarkMessageProcessed_NormalWamid_ProducesValidJson()
    {
        var session = BotSession.Create("abc123hash");

        session.MarkMessageProcessed("wamid.HBgNxyz123");

        var act = () => JsonDocument.Parse(session.ProcessedMessageIds);
        act.Should().NotThrow("ProcessedMessageIds must always be valid JSON");
    }

    [Fact]
    public void MarkMessageProcessed_WamidWithDoubleQuote_ProducesValidJson()
    {
        var session = BotSession.Create("abc123hash");
        var craftedWamid = "wamid.evil\"injection\"attempt";

        session.MarkMessageProcessed(craftedWamid);

        var act = () => JsonDocument.Parse(session.ProcessedMessageIds);
        act.Should().NotThrow("even a crafted wamid must not corrupt the JSON structure");
    }

    [Fact]
    public void MarkMessageProcessed_WamidWithDoubleQuote_IsFoundByIsMessageProcessed()
    {
        var session = BotSession.Create("abc123hash");
        var craftedWamid = "wamid.evil\"injection\"";

        session.MarkMessageProcessed(craftedWamid);

        session.IsMessageProcessed(craftedWamid).Should().BeTrue(
            "the exact crafted wamid must be detected as processed");
    }

    [Fact]
    public void IsMessageProcessed_WamidWithBackslash_ProducesValidJson()
    {
        var session = BotSession.Create("abc123hash");
        var craftedWamid = "wamid.back\\slash";

        session.MarkMessageProcessed(craftedWamid);

        var act = () => JsonDocument.Parse(session.ProcessedMessageIds);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsMessageProcessed_CraftedWamidThatLooksLikeTwoEntries_DoesNotFalselyMatch()
    {
        // Guard against a payload that tries to appear as if a second wamid was already
        // accepted by embedding the target wamid inside a crafted one.
        var session = BotSession.Create("abc123hash");
        session.MarkMessageProcessed("wamid.abc");

        // Attacker sends a wamid that contains the already-accepted id plus extra content
        var craftedWamid = "wamid.abc\",\"wamid.injected";

        // Even if MarkMessageProcessed is called, IsMessageProcessed for the crafted value
        // must return false for the crafted wamid if it hasn't been explicitly added.
        session.IsMessageProcessed(craftedWamid).Should().BeFalse(
            "a crafted wamid must not match via JSON-substring trickery");
    }

    [Fact]
    public void MarkMessageProcessed_SameWamidTwice_DeduplicatesInJson()
    {
        var session = BotSession.Create("abc123hash");

        session.MarkMessageProcessed("wamid.abc123");
        session.MarkMessageProcessed("wamid.abc123"); // duplicate

        var doc = JsonDocument.Parse(session.ProcessedMessageIds);
        doc.RootElement.GetArrayLength().Should().Be(1,
            "duplicate wamids must not be stored twice");
    }

    [Fact]
    public void MarkMessageProcessed_MultipleWamids_AllStoredAndRetrievable()
    {
        var session = BotSession.Create("abc123hash");

        session.MarkMessageProcessed("wamid.first");
        session.MarkMessageProcessed("wamid.second");
        session.MarkMessageProcessed("wamid.third");

        session.IsMessageProcessed("wamid.first").Should().BeTrue();
        session.IsMessageProcessed("wamid.second").Should().BeTrue();
        session.IsMessageProcessed("wamid.third").Should().BeTrue();
        session.IsMessageProcessed("wamid.notStored").Should().BeFalse();
    }
}
