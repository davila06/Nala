using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicProfileChangeDomainTests
{
    [Fact]
    public void Submit_StartsPendingAndApproveAppliesSnapshot()
    {
        var change = ClinicProfileChange.Submit(
            Guid.NewGuid(), Guid.NewGuid(),
            "{\"name\":\"Vet Nueva\"}");

        change.Status.Should().Be(ClinicProfileChangeStatus.Pending);
        change.Approve(Guid.NewGuid(), "Validado");
        change.Status.Should().Be(ClinicProfileChangeStatus.Approved);
        change.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void RejectRequiresReason()
    {
        var change = ClinicProfileChange.Submit(Guid.NewGuid(), Guid.NewGuid(), "{}");

        var result = change.Reject(Guid.NewGuid(), "");

        result.IsFailure.Should().BeTrue();
    }
}