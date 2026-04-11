using FluentValidation.TestHelper;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics.Validators;

public sealed class PerformClinicScanCommandValidatorTests
{
    private readonly PerformClinicScanCommandValidator _sut = new();

    private static PerformClinicScanCommand ValidQrCommand(string input = "https://pawtrack.cr/p/00000000-0000-7000-8000-000000000001") =>
        new(Guid.NewGuid(), input, ScanInputType.Qr);

    private static PerformClinicScanCommand ValidRfidCommand(string input = "982000361234567") =>
        new(Guid.NewGuid(), input, ScanInputType.RfidChip);

    [Fact]
    public void ValidQrInput_ShouldPass()
    {
        _sut.TestValidate(ValidQrCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRfidInput_ShouldPass()
    {
        _sut.TestValidate(ValidRfidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    public void Input_Empty_ShouldFail(string input)
    {
        _sut.TestValidate(ValidQrCommand(input))
            .ShouldHaveValidationErrorFor(x => x.Input);
    }

    [Fact]
    public void Input_TooLong_ShouldFail()
    {
        var tooLong = "https://pawtrack.cr/" + new string('a', 1010); // 20 + 1010 = 1030 > 1024
        _sut.TestValidate(ValidQrCommand(tooLong))
            .ShouldHaveValidationErrorFor(x => x.Input);
    }
}
