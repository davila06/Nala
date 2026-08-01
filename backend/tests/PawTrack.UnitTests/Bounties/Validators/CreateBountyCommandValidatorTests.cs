using FluentValidation.TestHelper;
using PawTrack.Application.Bounties.Commands.CreateBounty;

namespace PawTrack.UnitTests.Bounties.Validators;

public sealed class CreateBountyCommandValidatorTests
{
    private readonly CreateBountyCommandValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(4_999)]
    [InlineData(-1)]
    public void Amount_BelowMinimum_ShouldFail(decimal amount)
    {
        var cmd = new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), amount);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_AboveMaximum_ShouldFail()
    {
        var cmd = new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), 5_000_001m);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void LostPetEventId_Empty_ShouldFail()
    {
        var cmd = new CreateBountyCommand(Guid.Empty, Guid.NewGuid(), 25_000m);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.LostPetEventId);
    }

    [Theory]
    [InlineData("CRCC")]  // 4 chars
    [InlineData("CR")]    // 2 chars
    [InlineData("")]
    public void CurrencyCode_InvalidLength_ShouldFail(string code)
    {
        var cmd = new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), 25_000m, code);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), 25_000m, "CRC");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
