using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Application.Sightings.VisualMatch;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Sightings.Commands.ReportFoundPet;

public sealed class ReportFoundPetCommandHandler(
    IFoundPetRepository foundPetRepository,
    ILostPetRepository lostPetRepository,
    IUserRepository userRepository,
    IBlobStorageService blobStorageService,
    IImageProcessor imageProcessor,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportFoundPetCommand, Result<ReportFoundPetResult>>
{
    private const string BlobContainer = "found-pet-photos";
    private const int MatchThreshold = 70;

    /// <summary>Bounding box half-size in degrees — approximately 5 km at Costa Rica's latitude.</summary>
    private const double BboxHalfDeg = 0.045;

    public async Task<Result<ReportFoundPetResult>> Handle(
        ReportFoundPetCommand request, CancellationToken cancellationToken)
    {
        // 1. Persist new report (no photo yet so we get the ID first)
        var report = FoundPetReport.Create(
            request.FoundSpecies,
            request.BreedEstimate,
            request.ColorDescription,
            request.SizeEstimate,
            request.FoundLat,
            request.FoundLng,
            request.ContactName,
            request.ContactPhone,
            request.Note);

        await foundPetRepository.AddAsync(report, cancellationToken);

        // 2. Upload photo if provided
        if (request.PhotoStream is not null && request.PhotoContentType is not null)
        {
            using var ms = new MemoryStream();
            await request.PhotoStream.CopyToAsync(ms, cancellationToken);
            var rawBytes = ms.ToArray();

            var safeBytes = await imageProcessor.ResizeAsync(rawBytes, 800, cancellationToken);

            var blobName = $"{report.Id}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg";
            using var safeStream = new MemoryStream(safeBytes);
            var photoUrl = await blobStorageService.UploadAsync(
                BlobContainer, blobName, safeStream, "image/jpeg", cancellationToken);
            report.SetPhoto(photoUrl);
        }

        // 3. Save report to DB
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Find candidate lost-pet events in a ~5 km bounding box
        var north = request.FoundLat + BboxHalfDeg;
        var south = request.FoundLat - BboxHalfDeg;
        var east  = request.FoundLng + BboxHalfDeg;
        var west  = request.FoundLng - BboxHalfDeg;

        var candidates = await lostPetRepository.GetActiveLostPetsForMatchAsync(
            north, south, east, west, cancellationToken);

        // 5. Score and rank candidates
        var scored = candidates
            .Select(c => (Candidate: c, Score: ComputeScore(request, c)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        // 6. Auto-match and notify if top score exceeds threshold
        if (scored.Count > 0 && scored[0].Score >= MatchThreshold)
        {
            var topCandidate = scored[0].Candidate;
            var topScore = scored[0].Score;

            report.Match(topCandidate.LostPetEventId, topScore);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var owner = await userRepository.GetByIdAsync(topCandidate.OwnerId, cancellationToken);
            if (owner is not null)
            {
                await notificationDispatcher.DispatchFoundPetMatchAsync(
                    owner.Id,
                    owner.Email,
                    owner.Name,
                    topCandidate.PetName,
                    report.Id,
                    topScore,
                    cancellationToken);
            }
        }

        // 7. Return top 5 candidates to the caller
        var resultCandidates = scored
            .Take(5)
            .Select(x => new MatchCandidateDto(
                x.Candidate.LostPetEventId,
                x.Candidate.PetId,
                x.Candidate.PetName,
                x.Candidate.PetPhotoUrl,
                x.Candidate.LastSeenLat,
                x.Candidate.LastSeenLng,
                x.Candidate.ReportedAt,
                x.Score))
            .ToList();

        return Result.Success(new ReportFoundPetResult(report.Id, resultCandidates));
    }

    /// <summary>
    /// Scores a LostPetEvent candidate against the found-pet report.
    /// Maximum score: 100.
    ///   Species match:   0 or 40 pts
    ///   Distance (≤5km): 0–40 pts (linear decay)
    ///   Recency (≤30d):  0–20 pts (linear decay)
    /// </summary>
    private static int ComputeScore(ReportFoundPetCommand request, ActiveLostPetForMatchDto candidate)
    {
        var speciesScore = candidate.Species == request.FoundSpecies ? 40.0 : 0.0;

        double distanceScore = 0.0;
        if (candidate.LastSeenLat.HasValue && candidate.LastSeenLng.HasValue)
        {
            var distKm = VectorMath.HaversineKm(
                request.FoundLat, request.FoundLng,
                candidate.LastSeenLat.Value, candidate.LastSeenLng.Value);
            distanceScore = distKm < 5.0 ? (1.0 - distKm / 5.0) * 40.0 : 0.0;
        }

        var daysSinceLost = (DateTimeOffset.UtcNow - candidate.ReportedAt).TotalDays;
        var recencyScore = daysSinceLost <= 30.0 ? (1.0 - daysSinceLost / 30.0) * 20.0 : 0.0;

        return (int)Math.Round(speciesScore + distanceScore + recencyScore);
    }
}
