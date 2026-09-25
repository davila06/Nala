using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicalConsultationDomainTests
{
    [Fact]
    public void Create_WithSoapVitalsDiagnosisAndTreatment_StoresStructuredConsultation()
    {
        var consultation = ClinicalConsultation.Create(
            clinicId: Guid.NewGuid(),
            appointmentId: Guid.NewGuid(),
            petId: Guid.NewGuid(),
            veterinarianId: Guid.NewGuid(),
            ownerId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            reason: "Control general",
            subjective: "Come bien",
            objective: "Paciente alerta",
            assessment: "Condicion estable",
            plan: "Control en 6 meses",
            weightKg: 12.4m,
            temperatureC: 38.4m,
            heartRateBpm: 90,
            respiratoryRateRpm: 24,
            bodyConditionScore: 5,
            painScore: 1,
            hydrationStatus: "Normal",
            diagnosis: "Sano",
            treatment: "Vacuna anual",
            ownerSummary: "Nala esta estable.");

        consultation.Reason.Should().Be("Control general");
        consultation.Subjective.Should().Be("Come bien");
        consultation.WeightKg.Should().Be(12.4m);
        consultation.Diagnosis.Should().Be("Sano");
        consultation.Status.Should().Be(ClinicalConsultationStatus.Draft);
    }

    [Fact]
    public void Close_SignsConsultationAndPreventsSecondClose()
    {
        var consultation = ClinicalConsultation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Control", "S", "O", "A", "P", null, null, null, null, null, null, null,
            "Dx", "Tx", "Resumen");

        consultation.SetPrescription("Amoxicilina 250 mg cada 12 horas por 7 días.");

        consultation.Close(Guid.NewGuid(), "Dra. Ana Mora");

        consultation.Status.Should().Be(ClinicalConsultationStatus.Closed);
        consultation.PrescriptionInstructions.Should().Contain("Amoxicilina");
        consultation.SignedByName.Should().Be("Dra. Ana Mora");
        consultation.ClosedAt.Should().NotBeNull();
        var act = () => consultation.Close(Guid.NewGuid(), "Otra firma");
        act.Should().Throw<InvalidOperationException>();
    }
}
