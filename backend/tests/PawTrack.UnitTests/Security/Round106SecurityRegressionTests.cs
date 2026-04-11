using FluentValidation.TestHelper;
using PawTrack.Application.Incentives.Queries.GetLeaderboard;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R106 — GetLeaderboardQuery must have a formal pipeline validator for the
/// Take parameter. The handler's Math.Clamp is defense-in-depth; the
/// validator is the canonical enforcement point (fails fast, returns 422).
/// </summary>
public sealed class Round106SecurityRegressionTests
{
    private readonly GetLeaderboardQueryValidator _validator = new();

    [Fact]
    public void R106_TakeZero_Fails()
        => _validator.TestValidate(new GetLeaderboardQuery(0))
                     .ShouldHaveValidationErrorFor(x => x.Take);

    [Fact]
    public void R106_TakeNegative_Fails()
        => _validator.TestValidate(new GetLeaderboardQuery(-1))
                     .ShouldHaveValidationErrorFor(x => x.Take);

    [Fact]
    public void R106_TakeExceedsMax_Fails()
        => _validator.TestValidate(new GetLeaderboardQuery(51))
                     .ShouldHaveValidationErrorFor(x => x.Take);

    [Fact]
    public void R106_TakeOne_Passes()
        => _validator.TestValidate(new GetLeaderboardQuery(1))
                     .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void R106_TakeFifty_Passes()
        => _validator.TestValidate(new GetLeaderboardQuery(50))
                     .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void R106_TakeDefault_Passes()
        => _validator.TestValidate(new GetLeaderboardQuery())
                     .ShouldNotHaveAnyValidationErrors();
}
