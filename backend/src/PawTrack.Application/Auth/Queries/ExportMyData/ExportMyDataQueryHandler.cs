using MediatR;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Application.Medical;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Queries.ExportMyData;

public sealed class ExportMyDataQueryHandler(
    IUserRepository userRepository,
    IPetRepository petRepository,
    ILostPetRepository lostPetRepository,
    IMedicalRepository medicalRepository,
    IChatRepository chatRepository,
    INotificationRepository notificationRepository)
    : IRequestHandler<ExportMyDataQuery, Result<UserDataExportDto>>
{
    // Hard caps prevent memory exhaustion for accounts with unusually large history.
    private const int MaxMessagesPerThread = 1000;
    private const int MaxNotifications = 2000;

    public async Task<Result<UserDataExportDto>> Handle(
        ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserDataExportDto>("Usuario no encontrado.");

        var pets = await petRepository.GetByOwnerIdAsync(request.UserId, cancellationToken);
        var petDtos = pets.Select(PetSummaryDto.FromDomain).ToList().AsReadOnly();

        var lostPetEvents = await lostPetRepository.GetByOwnerIdAsync(request.UserId, cancellationToken);
        var lostPetDtos = lostPetEvents.Select(LostPetEventDto.FromDomain).ToList().AsReadOnly();

        var medicalRecords = new List<MedicalRecordDto>();
        foreach (var pet in pets)
        {
            var records = await medicalRepository.GetByPetIdAsync(pet.Id, cancellationToken);
            medicalRecords.AddRange(records.Select(MedicalRecordDto.FromDomain));
        }

        var chatMessages = new List<ChatMessageExportDto>();
        var threads = await chatRepository.GetThreadsByUserAsync(request.UserId, cancellationToken);
        foreach (var thread in threads)
        {
            var messages = await chatRepository.GetMessagesByThreadAsync(
                thread.Id, beforeMessageId: null, pageSize: MaxMessagesPerThread, cancellationToken);

            // Only the requesting user's own authored messages are exported — the other
            // party's messages are their own personal data, not the exporting user's.
            chatMessages.AddRange(messages
                .Where(m => m.SenderUserId == request.UserId)
                .Select(m => new ChatMessageExportDto(thread.Id, m.SentAt, m.Body)));
        }

        var notifications = await notificationRepository.GetByUserIdAsync(
            request.UserId, skip: 0, take: MaxNotifications, cancellationToken);
        var notificationDtos = notifications
            .Select(n => new NotificationExportDto(
                n.Id, n.Type.ToString(), n.Title, n.Body, n.IsRead, n.CreatedAt))
            .ToList()
            .AsReadOnly();

        var export = new UserDataExportDto(
            UserProfileDto.FromDomain(user),
            petDtos,
            lostPetDtos,
            medicalRecords.AsReadOnly(),
            chatMessages.AsReadOnly(),
            notificationDtos,
            DateTimeOffset.UtcNow);

        return Result.Success(export);
    }
}
