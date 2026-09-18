using System.Text.Json;
using FluentValidation;
using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.CastrationCampaigns;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.CastrationCampaigns.Commands.ReserveCastrationAppointment;

public sealed record ReserveCastrationAppointmentCommand(
    Guid RequestingUserId,
    Guid CampaignId,
    Guid PetId,
    DateTimeOffset ScheduledAt,
    decimal WeightKg,
    bool ConfirmsFastingInstructions,
    bool IsPregnant,
    bool IsInHeat,
    bool ConsentAccepted,
    string ConsentVersion,
    bool RequiresInvoice) : IRequest<Result<CastrationAppointmentDto>>;

public sealed class ReserveCastrationAppointmentCommandValidator
    : AbstractValidator<ReserveCastrationAppointmentCommand>
{
    public ReserveCastrationAppointmentCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.WeightKg).InclusiveBetween(0.5m, 150m);
        RuleFor(x => x.ConfirmsFastingInstructions).Equal(true);
        RuleFor(x => x.IsPregnant).Equal(false).WithMessage("Pregnant animals require direct veterinary evaluation.");
        RuleFor(x => x.IsInHeat).Equal(false).WithMessage("Animals in heat require direct veterinary evaluation.");
        RuleFor(x => x.ConsentAccepted).Equal(true);
        RuleFor(x => x.ConsentVersion).NotEmpty().MaximumLength(40);
    }
}

public sealed class ReserveCastrationAppointmentCommandHandler(
    ICastrationCampaignRepository campaignRepository,
    ICastrationAppointmentRepository appointmentRepository,
    IPetRepository petRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReserveCastrationAppointmentCommand, Result<CastrationAppointmentDto>>
{
    public async Task<Result<CastrationAppointmentDto>> Handle(
        ReserveCastrationAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result.Failure<CastrationAppointmentDto>("Campaign was not found.");

        var now = DateTimeOffset.UtcNow;
        if (campaign.Status != CastrationCampaignStatus.Published ||
            now < campaign.ReservationsOpenAt || now > campaign.ReservationsCloseAt)
        {
            return Result.Failure<CastrationAppointmentDto>("Campaign is not accepting reservations.");
        }

        if (request.ScheduledAt < campaign.StartsAt || request.ScheduledAt >= campaign.EndsAt)
            return Result.Failure<CastrationAppointmentDto>("Appointment time is outside the campaign schedule.");

        if (!string.Equals(request.ConsentVersion, campaign.ConsentVersion, StringComparison.Ordinal))
            return Result.Failure<CastrationAppointmentDto>("Consent version is outdated.");

        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.RequestingUserId)
            return Result.Failure<CastrationAppointmentDto>("Pet does not belong to the requesting user.");
        if (pet.Species is not (PetSpecies.Dog or PetSpecies.Cat))
            return Result.Failure<CastrationAppointmentDto>("Only dogs and cats are eligible for this campaign.");
        if (pet.SterilizedStatus == SterilizedStatus.Yes)
            return Result.Failure<CastrationAppointmentDto>("Pet is already registered as sterilized.");

        if (await appointmentRepository.ExistsForPetAsync(campaign.Id, pet.Id, cancellationToken))
            return Result.Failure<CastrationAppointmentDto>("Pet already has an appointment in this campaign.");
        if (campaign.AvailableCapacity <= 0)
            return Result.Failure<CastrationAppointmentDto>("Campaign capacity is exhausted.");

        var ivaAmount = request.RequiresInvoice
            ? Math.Round(campaign.BasePriceCrc * CabysCatalog.StandardIvaRate, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var totalAmount = campaign.BasePriceCrc + ivaAmount;
        var eligibilitySnapshot = JsonSerializer.Serialize(new
        {
            pet.Species,
            pet.Sex,
            request.WeightKg,
            request.ConfirmsFastingInstructions,
            request.IsPregnant,
            request.IsInHeat,
            Eligible = true,
            EvaluatedAt = now,
        });

        campaign.ReserveSlot();
        var appointment = CastrationAppointment.Create(
            campaign.Id,
            pet.Id,
            request.RequestingUserId,
            request.ScheduledAt,
            eligibilitySnapshot,
            campaign.ConsentVersion,
            now,
            campaign.BasePriceCrc,
            ivaAmount,
            totalAmount);

        campaignRepository.Update(campaign);
        await appointmentRepository.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CastrationAppointmentDto.FromDomain(appointment));
    }
}
