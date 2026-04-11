using FluentAssertions;
using PawTrack.Application.Sightings.Commands.ReportFoundPet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-56 security regression tests.
///
/// Gap: <c>ReportFoundPetCommandValidator</c> validates <c>ColorDescription</c>
/// (200 chars), <c>SizeEstimate</c> (50 chars), and <c>Note</c> (500 chars)
/// but has NO rule for <c>BreedEstimate</c>:
///
///   <code>
///   RuleFor(x => x.ColorDescription).MaximumLength(200)...
///   RuleFor(x => x.SizeEstimate).MaximumLength(50)...
///   RuleFor(x => x.Note).MaximumLength(500)...
///   // ← BreedEstimate MaximumLength(100) missing
///   </code>
///
/// The corresponding DB column is <c>.HasMaxLength(100)</c>.  Submitting a
/// <c>BreedEstimate</c> longer than 100 characters passes validation and reaches
/// the EF Core <c>INSERT</c>, which then throws a <c>DbUpdateException</c> that
/// surfaces as HTTP 500.
///
/// Fix:
///   Add <c>RuleFor(x => x.BreedEstimate).MaximumLength(100).When(x => x.BreedEstimate != null);</c>
///   (field is nullable, so the guard is required to avoid FluentValidation running
///   the length check on a null value).
/// </summary>
public sealed class Round56SecurityRegressionTests
{
    private readonly ReportFoundPetCommandValidator _sut = new();

    [Fact]
    public void Validator_RejectsBreedEstimate_Exceeding100Characters()
    {
        // 101 chars — one character more than the DB column allows
        var breedEstimate = new string('B', 101);

        var result = _sut.Validate(BuildCommand(breedEstimate: breedEstimate));

        result.IsValid.Should().BeFalse(
            "a BreedEstimate exceeding 100 characters exceeds the DB column definition " +
            "and would cause a DbUpdateException at the INSERT layer");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReportFoundPetCommand.BreedEstimate),
            "the BreedEstimate field must carry a MaximumLength error");
    }

    [Fact]
    public void Validator_AcceptsBreedEstimate_AtColumnLimit()
    {
        // Exactly 100 chars — at the DB column edge
        var breedEstimate = new string('B', 100);

        var result = _sut.Validate(BuildCommand(breedEstimate: breedEstimate));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(ReportFoundPetCommand.BreedEstimate)
                          && e.ErrorMessage.Contains("100"),
                "a 100-character BreedEstimate is within the DB column limit");
    }

    [Fact]
    public void Validator_AcceptsNullBreedEstimate()
    {
        // BreedEstimate is optional — null must be accepted without error
        var result = _sut.Validate(BuildCommand(breedEstimate: null));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(ReportFoundPetCommand.BreedEstimate),
                "BreedEstimate is an optional field and null must be valid");
    }

    [Fact]
    public void Validator_AcceptsTypicalBreedEstimate()
    {
        var result = _sut.Validate(BuildCommand(breedEstimate: "Labrador Retriever"));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(ReportFoundPetCommand.BreedEstimate));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReportFoundPetCommand BuildCommand(string? breedEstimate = null) =>
        new(PetSpecies.Dog,
            breedEstimate,
            "Brown and white",
            "Medium",
            9.93, -84.08,
            "Juan Pérez",
            "+506-8888-9999",
            null,
            null,
            null);
}
