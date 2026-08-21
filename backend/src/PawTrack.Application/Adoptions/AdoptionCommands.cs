using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Adoptions;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AdoptablePetDto(
    string Id,
    string OrganizationUserId,
    string OrganizationName,
    string Name,
    string Species,
    string? Breed,
    string Size,
    string AgeCategory,
    int? AgeMonthsApprox,
    string Story,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard,
    double RefLat,
    double RefLng,
    string? RefLabel,
    string Status,
    IReadOnlyList<string> PhotoUrls,
    DateTimeOffset PublishedAt)
{
    public static AdoptablePetDto FromDomain(AdoptablePet p, string organizationName) => new(
        p.Id.ToString(),
        p.OrganizationUserId.ToString(),
        organizationName,
        p.Name,
        p.Species.ToString(),
        p.Breed,
        p.Size.ToString(),
        p.AgeCategory.ToString(),
        p.AgeMonthsApprox,
        p.Story,
        p.Requirements,
        p.MedicalNotes,
        p.IsVaccinated,
        p.IsSterilized,
        p.IsMicrochipped,
        p.OkWithKids,
        p.OkWithDogs,
        p.OkWithCats,
        p.NeedsYard,
        p.RefLat,
        p.RefLng,
        p.RefLabel,
        p.Status.ToString(),
        p.PhotoUrls,
        p.PublishedAt);
}

public sealed record AdoptionApplicationDto(
    string Id,
    string AdoptablePetId,
    string ApplicantUserId,
    string ApplicantNote,
    string Status,
    string? ReviewNote,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt);

public sealed record AdoptionFairDto(
    string Id,
    string OrganizationUserId,
    string Title,
    string? Description,
    string VenueLabel,
    double Lat,
    double Lng,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    IReadOnlyList<string> AnimalIds)
{
    public static AdoptionFairDto FromDomain(AdoptionFair f) => new(
        f.Id.ToString(),
        f.OrganizationUserId.ToString(),
        f.Title,
        f.Description,
        f.VenueLabel,
        f.Lat,
        f.Lng,
        f.StartsAt,
        f.EndsAt,
        f.Status.ToString(),
        f.AnimalIds.Select(id => id.ToString()).ToList().AsReadOnly());
}

// ── Publish animal ────────────────────────────────────────────────────────────

public sealed record PublishAdoptablePetCommand(
    Guid OrganizationUserId,
    string Name,
    PetSpecies Species,
    PetSize Size,
    AgeCategory AgeCategory,
    string Story,
    double RefLat,
    double RefLng,
    string? RefLabel,
    string? Breed,
    int? AgeMonthsApprox,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard) : IRequest<Result<AdoptablePetDto>>;

public sealed class PublishAdoptablePetCommandValidator : AbstractValidator<PublishAdoptablePetCommand>
{
    public PublishAdoptablePetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Story).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Requirements).MaximumLength(500);
        RuleFor(x => x.MedicalNotes).MaximumLength(500);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.RefLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.RefLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.AgeMonthsApprox).GreaterThan(0).When(x => x.AgeMonthsApprox.HasValue);
    }
}

public sealed class PublishAdoptablePetCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IAdoptionRepository adoptionRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork,
    ILogger<PublishAdoptablePetCommandHandler> logger)
    : IRequestHandler<PublishAdoptablePetCommand, Result<AdoptablePetDto>>
{
    public const string NotVerifiedShelterError = "not_verified_shelter";
    public const string ShelterBasicLimitError  = "shelter_basic_limit_reached";

    public async Task<Result<AdoptablePetDto>> Handle(
        PublishAdoptablePetCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>(NotVerifiedShelterError);

        // ShelterBasic is limited to 5 active animals; ShelterPlus is unlimited
        var tier = await subscriptionService.GetActiveUserTierAsync(request.OrganizationUserId, ct);
        if (tier < SubscriptionTier.ShelterPlus)
        {
            var activeCount = await adoptionRepository.CountByOrganizationAsync(request.OrganizationUserId, ct);
            if (activeCount >= 5)
                return Result.Failure<AdoptablePetDto>(ShelterBasicLimitError);
        }
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>(NotVerifiedShelterError);

        var animal = AdoptablePet.Create(
            request.OrganizationUserId, request.Name, request.Species,
            request.Size, request.AgeCategory, request.Story,
            request.RefLat, request.RefLng, request.RefLabel,
            request.Breed, request.AgeMonthsApprox, request.Requirements,
            request.MedicalNotes, request.IsVaccinated, request.IsSterilized,
            request.IsMicrochipped, request.OkWithKids, request.OkWithDogs,
            request.OkWithCats, request.NeedsYard);

        await adoptionRepository.AddAnimalAsync(animal, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Animal {AnimalId} published for adoption by shelter {ShelterId}",
            animal.Id, request.OrganizationUserId);

        return Result.Success(AdoptablePetDto.FromDomain(animal, ally.OrganizationName));
    }
}

// ── Update animal details ─────────────────────────────────────────────────────

public sealed record UpdateAdoptablePetCommand(
    Guid OrganizationUserId,
    Guid AnimalId,
    string Name,
    string Story,
    string? Requirements,
    string? MedicalNotes,
    bool IsVaccinated,
    bool IsSterilized,
    bool IsMicrochipped,
    bool OkWithKids,
    bool OkWithDogs,
    bool OkWithCats,
    bool NeedsYard) : IRequest<Result<AdoptablePetDto>>;

public sealed class UpdateAdoptablePetCommandValidator : AbstractValidator<UpdateAdoptablePetCommand>
{
    public UpdateAdoptablePetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Story).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Requirements).MaximumLength(500);
        RuleFor(x => x.MedicalNotes).MaximumLength(500);
    }
}

public sealed class UpdateAdoptablePetCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IAdoptionRepository adoptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAdoptablePetCommand, Result<AdoptablePetDto>>
{
    public async Task<Result<AdoptablePetDto>> Handle(
        UpdateAdoptablePetCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AnimalId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<AdoptablePetDto>("access_denied");

        animal.UpdateDetails(
            request.Name, request.Story, request.Requirements, request.MedicalNotes,
            request.IsVaccinated, request.IsSterilized, request.IsMicrochipped,
            request.OkWithKids, request.OkWithDogs, request.OkWithCats, request.NeedsYard);

        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(AdoptablePetDto.FromDomain(animal, ally.OrganizationName));
    }
}

// ── Upload photo ──────────────────────────────────────────────────────────────

public sealed record UploadAdoptionPhotoCommand(
    Guid OrganizationUserId,
    Guid AnimalId,
    Stream PhotoStream,
    string ContentType,
    string FileName) : IRequest<Result<string>>;

public sealed class UploadAdoptionPhotoCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IAdoptionRepository adoptionRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadAdoptionPhotoCommand, Result<string>>
{
    public const string MaxPhotosError = "max_photos_reached";

    public async Task<Result<string>> Handle(UploadAdoptionPhotoCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<string>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AnimalId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<string>("access_denied");

        if (animal.PhotoUrls.Count >= 5)
            return Result.Failure<string>(MaxPhotosError);

        var sanitized = BlobHelper.SanitizeFileName(request.FileName);
        var blobName = $"adoption-photos/{animal.Id}/{Guid.CreateVersion7()}-{sanitized}";
        var url = await blobStorage.UploadAsync("adoption-photos", blobName, request.PhotoStream, request.ContentType, ct);

        animal.AddPhoto(url);
        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(url);
    }
}

// ── Delete photo ──────────────────────────────────────────────────────────────

public sealed record DeleteAdoptionPhotoCommand(
    Guid OrganizationUserId,
    Guid AnimalId,
    string PhotoUrl) : IRequest<Result<bool>>;

public sealed class DeleteAdoptionPhotoCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IAdoptionRepository adoptionRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAdoptionPhotoCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteAdoptionPhotoCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<bool>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AnimalId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<bool>("access_denied");

        if (!animal.PhotoUrls.Contains(request.PhotoUrl))
            return Result.Failure<bool>("photo_not_found");

        // Extract blob path from full URL and delete from storage
        var uri = new Uri(request.PhotoUrl);
        var blobPath = uri.AbsolutePath.TrimStart('/');
        await blobStorage.DeleteAsync(blobPath, ct);

        animal.RemovePhoto(request.PhotoUrl);
        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}

// ── Apply to adopt ────────────────────────────────────────────────────────────

public sealed record ApplyToAdoptCommand(
    Guid ApplicantUserId,
    Guid AdoptablePetId,
    string ApplicantNote) : IRequest<Result<AdoptionApplicationDto>>;

public sealed class ApplyToAdoptCommandValidator : AbstractValidator<ApplyToAdoptCommand>
{
    public ApplyToAdoptCommandValidator()
    {
        RuleFor(x => x.ApplicantNote).NotEmpty().MaximumLength(500);
    }
}

public sealed class ApplyToAdoptCommandHandler(
    IAdoptionRepository adoptionRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<ApplyToAdoptCommandHandler> logger)
    : IRequestHandler<ApplyToAdoptCommand, Result<AdoptionApplicationDto>>
{
    public const string AnimalNotFoundError = "animal_not_found";
    public const string AnimalNotAvailableError = "animal_not_available";
    public const string DuplicateApplicationError = "duplicate_application";

    public async Task<Result<AdoptionApplicationDto>> Handle(
        ApplyToAdoptCommand request, CancellationToken ct)
    {
        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AdoptablePetId, ct);
        if (animal is null)
            return Result.Failure<AdoptionApplicationDto>(AnimalNotFoundError);

        if (animal.Status != AdoptionStatus.Available)
            return Result.Failure<AdoptionApplicationDto>(AnimalNotAvailableError);

        var existing = await adoptionRepository.GetApplicationByApplicantAndAnimalAsync(
            request.ApplicantUserId, request.AdoptablePetId, ct);
        if (existing is not null && existing.Status == ApplicationStatus.Pending)
            return Result.Failure<AdoptionApplicationDto>(DuplicateApplicationError);

        var application = AdoptionApplication.Create(
            request.AdoptablePetId, request.ApplicantUserId, request.ApplicantNote);

        await adoptionRepository.AddApplicationAsync(application, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _ = notificationDispatcher.DispatchAdoptionInterestAsync(
                animal.OrganizationUserId, animal.Name, application.Id, ct)
            .ContinueWith(t => logger.LogWarning(t.Exception,
                "Adoption interest notification failed for app {AppId}", application.Id),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(new AdoptionApplicationDto(
            application.Id.ToString(), application.AdoptablePetId.ToString(), application.ApplicantUserId.ToString(),
            application.ApplicantNote, application.Status.ToString(), application.ReviewNote,
            application.AppliedAt, application.ReviewedAt));
    }
}

// ── Withdraw application ──────────────────────────────────────────────────────

public sealed record WithdrawApplicationCommand(
    Guid ApplicantUserId,
    Guid ApplicationId) : IRequest<Result<bool>>;

public sealed class WithdrawApplicationCommandHandler(
    IAdoptionRepository adoptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<WithdrawApplicationCommand, Result<bool>>
{
    public const string NotOwnApplicationError = "not_own_application";

    public async Task<Result<bool>> Handle(
        WithdrawApplicationCommand request, CancellationToken ct)
    {
        var application = await adoptionRepository.GetApplicationByIdAsync(request.ApplicationId, ct);
        if (application is null)
            return Result.Failure<bool>("application_not_found");

        if (application.ApplicantUserId != request.ApplicantUserId)
            return Result.Failure<bool>(NotOwnApplicationError);

        try
        {
            application.Withdraw();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<bool>(ex.Message);
        }

        adoptionRepository.UpdateApplication(application);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}

// ── Review application (shelter) ─────────────────────────────────────────────

public sealed record ReviewAdoptionApplicationCommand(
    Guid OrganizationUserId,
    Guid ApplicationId,
    bool Approve,
    string? ReviewNote) : IRequest<Result<AdoptionApplicationDto>>;

public sealed class ReviewAdoptionApplicationCommandValidator
    : AbstractValidator<ReviewAdoptionApplicationCommand>
{
    public ReviewAdoptionApplicationCommandValidator()
    {
        RuleFor(x => x.ReviewNote).MaximumLength(300);
    }
}

public sealed class ReviewAdoptionApplicationCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<ReviewAdoptionApplicationCommandHandler> logger)
    : IRequestHandler<ReviewAdoptionApplicationCommand, Result<AdoptionApplicationDto>>
{
    public async Task<Result<AdoptionApplicationDto>> Handle(
        ReviewAdoptionApplicationCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptionApplicationDto>("not_verified_shelter");

        var application = await adoptionRepository.GetApplicationByIdAsync(request.ApplicationId, ct);
        if (application is null)
            return Result.Failure<AdoptionApplicationDto>("application_not_found");

        var animal = await adoptionRepository.GetAnimalByIdAsync(application.AdoptablePetId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<AdoptionApplicationDto>("access_denied");

        if (request.Approve)
        {
            application.Approve(request.ReviewNote);
            animal.MarkInProcess();
            adoptionRepository.UpdateAnimal(animal);

            _ = notificationDispatcher.DispatchAdoptionApprovedAsync(
                    application.ApplicantUserId, animal.Name, application.Id, ct)
                .ContinueWith(t => logger.LogWarning(t.Exception,
                    "AdoptionApproved notification failed for app {AppId}", application.Id),
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
        else
        {
            application.Reject(request.ReviewNote);

            _ = notificationDispatcher.DispatchAdoptionRejectedAsync(
                    application.ApplicantUserId, animal.Name, application.Id, ct)
                .ContinueWith(t => logger.LogWarning(t.Exception,
                    "AdoptionRejected notification failed for app {AppId}", application.Id),
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        adoptionRepository.UpdateApplication(application);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AdoptionApplicationDto(
            application.Id.ToString(), application.AdoptablePetId.ToString(), application.ApplicantUserId.ToString(),
            application.ApplicantNote, application.Status.ToString(), application.ReviewNote,
            application.AppliedAt, application.ReviewedAt));
    }
}

// ── Mark adopted ─────────────────────────────────────────────────────────────

public sealed record MarkAdoptedCommand(
    Guid OrganizationUserId,
    Guid AnimalId) : IRequest<Result<AdoptablePetDto>>;

public sealed class MarkAdoptedCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkAdoptedCommand, Result<AdoptablePetDto>>
{
    public async Task<Result<AdoptablePetDto>> Handle(
        MarkAdoptedCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptablePetDto>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AnimalId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<AdoptablePetDto>("access_denied");

        animal.MarkAdopted();
        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(AdoptablePetDto.FromDomain(animal, ally.OrganizationName));
    }
}

// ── Create fair ───────────────────────────────────────────────────────────────

public sealed record CreateAdoptionFairCommand(
    Guid OrganizationUserId,
    string Title,
    string VenueLabel,
    double Lat,
    double Lng,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Description,
    IReadOnlyList<Guid> AnimalIds) : IRequest<Result<AdoptionFairDto>>;

public sealed class CreateAdoptionFairCommandValidator : AbstractValidator<CreateAdoptionFairCommand>
{
    public CreateAdoptionFairCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.VenueLabel).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.StartsAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class CreateAdoptionFairCommandHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository,
    ISubscriptionService subscriptionService,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<CreateAdoptionFairCommandHandler> logger)
    : IRequestHandler<CreateAdoptionFairCommand, Result<AdoptionFairDto>>
{
    public const string ShelterPlusRequiredError = "shelter_plus_required";

    public async Task<Result<AdoptionFairDto>> Handle(
        CreateAdoptionFairCommand request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<AdoptionFairDto>("not_verified_shelter");

        // Fairs require ShelterPlus subscription
        var tier = await subscriptionService.GetActiveUserTierAsync(request.OrganizationUserId, ct);
        if (tier < SubscriptionTier.ShelterPlus)
            return Result.Failure<AdoptionFairDto>(ShelterPlusRequiredError);

        var fair = AdoptionFair.Create(
            request.OrganizationUserId, request.Title, request.VenueLabel,
            request.Lat, request.Lng, request.StartsAt, request.EndsAt, request.Description);

        foreach (var animalId in request.AnimalIds)
            fair.AddAnimal(animalId);

        await adoptionRepository.AddFairAsync(fair, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _ = notificationDispatcher.DispatchAdoptionFairAlertAsync(
                fair.Id, fair.Title, fair.Lat, fair.Lng,
                radiusMetres: 10_000, fair.StartsAt, ct)
            .ContinueWith(t => logger.LogWarning(t.Exception, "AdoptionFair geofence alert failed"),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(AdoptionFairDto.FromDomain(fair));
    }
}

// ── Shared helper ───────────────────────────────────────────────────────────────

file static class AdoptionCommandHelper
{
    internal static AdoptionApplicationDto ToDto(AdoptionApplication a) => new(
        a.Id.ToString(), a.AdoptablePetId.ToString(), a.ApplicantUserId.ToString(),
        a.ApplicantNote, a.Status.ToString(), a.ReviewNote, a.AppliedAt, a.ReviewedAt);
}
