using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetClinicalConsultationTemplates;

public sealed record ClinicalConsultationTemplateDto(
    string Key,
    string Label,
    string Reason,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan,
    string Diagnosis,
    string Treatment,
    string OwnerSummary,
    string? PrescriptionInstructions);

public sealed record GetClinicalConsultationTemplatesQuery
    : IRequest<Result<IReadOnlyList<ClinicalConsultationTemplateDto>>>;

public sealed class GetClinicalConsultationTemplatesQueryHandler
    : IRequestHandler<GetClinicalConsultationTemplatesQuery, Result<IReadOnlyList<ClinicalConsultationTemplateDto>>>
{
    private static readonly IReadOnlyList<ClinicalConsultationTemplateDto> Templates =
    [
        new(
            "checkup",
            "Consulta general",
            "Control general",
            "El propietario refiere evolución general y comportamiento habitual.",
            "Paciente alerta, hidratación y condición general evaluadas.",
            "Paciente clínicamente estable según evaluación.",
            "Mantener controles preventivos y seguimiento según evolución.",
            "Evaluación clínica general",
            "Indicaciones preventivas y seguimiento.",
            "La mascota fue valorada y se explicaron las indicaciones de seguimiento.",
            null),
        new(
            "vaccine",
            "Vacunación",
            "Vacunación / refuerzo",
            "Sin signos reportados que contraindiquen vacunación.",
            "Paciente evaluado previo a aplicación de vacuna.",
            "Apto para vacunación según criterio clínico.",
            "Observar por reacciones y programar próximo refuerzo.",
            "Inmunización preventiva",
            "Vacuna aplicada según esquema indicado.",
            "Se aplicó vacuna/refuerzo y se indicó vigilancia posterior.",
            "No administrar medicamentos adicionales salvo indicación veterinaria."),
        new(
            "surgery",
            "Cirugía / procedimiento",
            "Procedimiento quirúrgico",
            "Motivo quirúrgico informado por propietario y/o evaluación previa.",
            "Paciente preparado y evaluado para procedimiento.",
            "Procedimiento realizado con seguimiento postoperatorio requerido.",
            "Reposo, control de herida y seguimiento en fecha indicada.",
            "Postoperatorio",
            "Manejo postoperatorio e indicaciones de cuidado en casa.",
            "Se entregaron indicaciones postoperatorias y señales de alarma.",
            "Administrar medicación únicamente según dosis y frecuencia indicadas."),
        new(
            "dermatology",
            "Dermatología",
            "Consulta dermatológica",
            "Se reportan signos dermatológicos o prurito.",
            "Se evalúa piel, pelaje, lesiones visibles y distribución.",
            "Cuadro dermatológico en evaluación.",
            "Tratamiento inicial y control de evolución.",
            "Alteración dermatológica",
            "Tratamiento dermatológico indicado.",
            "Se explicaron cuidados de piel y seguimiento requerido.",
            null),
        new(
            "emergency",
            "Emergencia",
            "Atención de emergencia",
            "Motivo de emergencia reportado por propietario o tercero.",
            "Paciente evaluado con prioridad clínica y signos vitales registrados.",
            "Caso de emergencia con manejo inicial.",
            "Seguimiento estricto y escalamiento si aparecen signos de alarma.",
            "Emergencia veterinaria",
            "Manejo inicial de emergencia.",
            "Se explicó el estado actual, manejo indicado y señales de alarma.",
            null),
    ];

    public Task<Result<IReadOnlyList<ClinicalConsultationTemplateDto>>> Handle(
        GetClinicalConsultationTemplatesQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(Templates));
}
