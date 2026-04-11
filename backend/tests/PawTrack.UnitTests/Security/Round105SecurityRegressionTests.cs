using FluentValidation.TestHelper;
using PawTrack.Application.Bot.Queries.VerifyWhatsAppWebhook;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R105 — VerifyWhatsAppWebhookQuery string length validation.
/// HubMode, HubVerifyToken, and HubChallenge must be bounded to
/// prevent oversized payloads reaching the handler.
/// </summary>
public sealed class Round105SecurityRegressionTests
{
    private readonly VerifyWhatsAppWebhookQueryValidator _validator = new();

    private static string Long(int n) => new('x', n);

    private static VerifyWhatsAppWebhookQuery Valid() =>
        new("subscribe", "my-verify-token", "challenge123");

    [Fact]
    public void R105_ValidQuery_Passes()
        => _validator.TestValidate(Valid())
                     .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void R105_EmptyHubMode_Fails()
        => _validator.TestValidate(Valid() with { HubMode = "" })
                     .ShouldHaveValidationErrorFor(x => x.HubMode);

    [Fact]
    public void R105_HubModeExceedsMaxLength_Fails()
        => _validator.TestValidate(Valid() with { HubMode = Long(256) })
                     .ShouldHaveValidationErrorFor(x => x.HubMode);

    [Fact]
    public void R105_EmptyHubVerifyToken_Fails()
        => _validator.TestValidate(Valid() with { HubVerifyToken = "" })
                     .ShouldHaveValidationErrorFor(x => x.HubVerifyToken);

    [Fact]
    public void R105_HubVerifyTokenExceedsMaxLength_Fails()
        => _validator.TestValidate(Valid() with { HubVerifyToken = Long(513) })
                     .ShouldHaveValidationErrorFor(x => x.HubVerifyToken);

    [Fact]
    public void R105_EmptyHubChallenge_Fails()
        => _validator.TestValidate(Valid() with { HubChallenge = "" })
                     .ShouldHaveValidationErrorFor(x => x.HubChallenge);

    [Fact]
    public void R105_HubChallengeExceedsMaxLength_Fails()
        => _validator.TestValidate(Valid() with { HubChallenge = Long(513) })
                     .ShouldHaveValidationErrorFor(x => x.HubChallenge);
}
