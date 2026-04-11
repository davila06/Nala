using FluentAssertions;
using PawTrack.Application.Clinics.DTOs;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-18 security regression tests.
///
/// Gap: <c>ClinicScanResultDto</c> exposes <c>OwnerEmail</c> to any authenticated
/// clinic user who scans a pet QR code or RFID chip via
/// <c>POST /api/clinics/scan</c>.
///
/// Attack vector:
///   1. An attacker registers (or compromises) a clinic account — requires only
///      admin approval once; afterwards the JWT is long-lived.
///   2. The attacker systematically scans pet QR codes (posted on social media,
///      photographed from sighting flyers, or decoded from RFID tags).
///   3. Each scan response returns { OwnerEmail, OwnerName, PetName, … }.
///   4. The harvested emails enable: phishing campaigns, spam, account takeover
///      attempts on other platforms, and cross-platform identity linking
///      (email → full name → pet → GPS history).
///
/// Root cause: The owner notification is already dispatched server-side via
/// <c>DispatchClinicScanDetectedAsync</c>.  The clinic UI has no legitimate
/// need to receive the owner's raw email — the contact is mediated by the
/// platform's own notification pipeline.
///
/// Fix: Remove <c>OwnerEmail</c> from <c>ClinicScanResultDto</c> and replace
/// with a <c>bool OwnerNotified</c> flag so the clinic UI can confirm the owner
/// was contacted without receiving PII.
/// </summary>
public sealed class Round18SecurityRegressionTests
{
    // ── ClinicScanResultDto structure ─────────────────────────────────────────

    [Fact]
    public void ClinicScanResultDto_HasNoOwnerEmailProperty()
    {
        // Any active clinic account can harvest pet owner emails by scanning QR codes.
        // The server already notifies the owner — the client does not need the raw email.
        typeof(ClinicScanResultDto)
            .GetProperty("OwnerEmail")
            .Should().BeNull(
                "ClinicScanResultDto must not expose the owner's email address to clinic users — " +
                "the notification is already dispatched server-side; returning the email enables " +
                "phishing, spam, and cross-platform identity linking via the clinic scan endpoint");
    }

    [Fact]
    public void ClinicScanResultDto_HasOwnerNotifiedFlag()
    {
        // Replace OwnerEmail with a boolean confirming server-side notification occurred.
        typeof(ClinicScanResultDto)
            .GetProperty(nameof(ClinicScanResultDto.OwnerNotified))
            .Should().NotBeNull(
                "OwnerNotified boolean must be present so the clinic UI can confirm " +
                "the owner was contacted without receiving PII");

        typeof(ClinicScanResultDto)
            .GetProperty(nameof(ClinicScanResultDto.OwnerNotified))!
            .PropertyType
            .Should().Be(typeof(bool),
                "OwnerNotified must be a non-nullable bool — " +
                "null would mean the scan result is ambiguous about notification status");
    }

    [Fact]
    public void ClinicScanResultDto_PreservesNonSensitiveFields()
    {
        // Ensure safe fields are still present after the removal.
        typeof(ClinicScanResultDto).GetProperty(nameof(ClinicScanResultDto.ScanId))
            .Should().NotBeNull("ScanId must be preserved — needed for audit/log correlation");

        typeof(ClinicScanResultDto).GetProperty(nameof(ClinicScanResultDto.Matched))
            .Should().NotBeNull("Matched flag must be preserved");

        typeof(ClinicScanResultDto).GetProperty(nameof(ClinicScanResultDto.PetName))
            .Should().NotBeNull("PetName must be preserved — the clinic needs to know whose pet it is");

        typeof(ClinicScanResultDto).GetProperty(nameof(ClinicScanResultDto.OwnerName))
            .Should().NotBeNull(
                "OwnerName (display name only) must be preserved — " +
                "the clinic needs to greet the owner when they arrive; " +
                "a display name is NOT equivalent to an email address for enumeration purposes");

        typeof(ClinicScanResultDto).GetProperty(nameof(ClinicScanResultDto.PetSpecies))
            .Should().NotBeNull("PetSpecies must be preserved");
    }
}
