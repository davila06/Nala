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
    public void InternalTask_PersistsPriorityRoleAssigneeAndIdempotencyKeyWithoutPetContext()
    {
        var clinicId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var task = ClinicCrmTask.Create(
            clinicId,
            null,
            null,
            ClinicCrmTaskType.CloseCash,
            new DateOnly(2026, 9, 25),
            "Cerrar caja",
            null,
            creatorId,
            ClinicInternalTaskRole.Cashier,
            ClinicCrmTaskPriority.High,
            assigneeId,
            idempotencyKey);

        task.PetId.Should().BeNull();
        task.OwnerUserId.Should().BeNull();
        task.AssignedRole.Should().Be(ClinicInternalTaskRole.Cashier);
        task.Priority.Should().Be(ClinicCrmTaskPriority.High);
        task.AssignedToUserId.Should().Be(assigneeId);
        task.IdempotencyKey.Should().Be(idempotencyKey);
    }

    [Fact]
    public void InternalTask_RejectsAnEmptyIdempotencyKey()
    {
        var act = () => ClinicCrmTask.Create(
            Guid.NewGuid(), null, null, ClinicCrmTaskType.ReviewOperations,
            new DateOnly(2026, 9, 25), "Revisar operación", null, Guid.NewGuid(),
            ClinicInternalTaskRole.Manager, ClinicCrmTaskPriority.Normal, null, Guid.Empty);

        act.Should().Throw<ArgumentException>();
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
