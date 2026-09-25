using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicCrmDomainTests
{
    [Fact]
    public void Preference_Update_RecordsOptInSourceAndActor()
    {
        var preference = ClinicClientCommunicationPreference.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ClinicCommunicationChannel.WhatsApp,
            ClinicCommunicationPurpose.VaccineReminder,
            true,
            "formulario firmado",
            Guid.NewGuid());

        preference.IsOptedIn.Should().BeTrue();
        preference.ConsentSource.Should().Be("formulario firmado");
        preference.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Task_Complete_ClosesOnlyOpenTasks()
    {
        var task = ClinicCrmTask.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ClinicCrmTaskType.FollowUpTreatment,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Llamar por evolución",
            null,
            Guid.NewGuid());

        task.Complete(Guid.NewGuid());

        task.Status.Should().Be(ClinicCrmTaskStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
        var act = () => task.Complete(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProviderMessage_MustHaveReceiptBeforeSentState()
    {
        var activity = ClinicClientCommunicationActivity.Log(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp,
            ClinicCommunicationDirection.Outbound, ClinicCommunicationStatus.Queued,
            "Seguimiento", "Hola", null, Guid.NewGuid());

        var withoutReceipt = () => activity.MarkProviderAccepted("");
        withoutReceipt.Should().Throw<ArgumentException>();
        activity.Status.Should().Be(ClinicCommunicationStatus.Queued);

        activity.MarkProviderAccepted("provider-id");
        activity.Status.Should().Be(ClinicCommunicationStatus.Sent);
        activity.ProviderMessageId.Should().Be("provider-id");
    }
}
