SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [AllyProfiles] (
    [UserId] uniqueidentifier NOT NULL,
    [OrganizationName] nvarchar(120) NOT NULL,
    [AllyType] nvarchar(40) NOT NULL,
    [CoverageLabel] nvarchar(120) NOT NULL,
    [CoverageLat] float NOT NULL,
    [CoverageLng] float NOT NULL,
    [CoverageRadiusMetres] int NOT NULL,
    [VerificationStatus] nvarchar(20) NOT NULL,
    [AppliedAt] datetimeoffset NOT NULL,
    [VerifiedAt] datetimeoffset NULL,
    CONSTRAINT [PK_AllyProfiles] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [BotSessions] (
    [Id] uniqueidentifier NOT NULL,
    [PhoneNumberHash] nvarchar(64) NOT NULL,
    [Step] nvarchar(30) NOT NULL,
    [PetName] nvarchar(100) NULL,
    [LastSeenRaw] nvarchar(200) NULL,
    [LastSeenAt] datetimeoffset NULL,
    [LocationRaw] nvarchar(300) NULL,
    [LastSeenLat] float NULL,
    [LastSeenLng] float NULL,
    [GuestUserId] uniqueidentifier NULL,
    [PetId] uniqueidentifier NULL,
    [LostEventId] uniqueidentifier NULL,
    [ProcessedMessageIds] nvarchar(2000) NOT NULL DEFAULT N'[]',
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_BotSessions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BroadcastAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NOT NULL,
    [Channel] nvarchar(30) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ExternalId] nvarchar(200) NULL,
    [TrackingUrl] nvarchar(500) NULL,
    [TrackingClicks] int NOT NULL DEFAULT 0,
    [ErrorMessage] nvarchar(500) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [SentAt] datetimeoffset NULL,
    CONSTRAINT [PK_BroadcastAttempts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ChatThreads] (
    [Id] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NOT NULL,
    [InitiatorUserId] uniqueidentifier NOT NULL,
    [OwnerUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [LastMessageAt] datetimeoffset NOT NULL,
    [FlagReason] nvarchar(500) NULL,
    CONSTRAINT [PK_ChatThreads] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Clinics] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [LicenseNumber] nvarchar(50) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Lat] decimal(9,6) NOT NULL,
    [Lng] decimal(9,6) NOT NULL,
    [ContactEmail] nvarchar(200) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RegisteredAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ClinicScans] (
    [Id] uniqueidentifier NOT NULL,
    [ClinicId] uniqueidentifier NOT NULL,
    [MatchedPetId] uniqueidentifier NULL,
    [ScanInput] nvarchar(2000) NOT NULL,
    [InputType] nvarchar(20) NOT NULL,
    [ScannedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ClinicScans] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ContributorScores] (
    [UserId] uniqueidentifier NOT NULL,
    [OwnerName] nvarchar(200) NOT NULL,
    [ReunificationCount] int NOT NULL,
    [Badge] nvarchar(20) NOT NULL,
    [TotalPoints] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ContributorScores] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [CustodyRecords] (
    [Id] uniqueidentifier NOT NULL,
    [FosterUserId] uniqueidentifier NOT NULL,
    [FoundPetReportId] uniqueidentifier NOT NULL,
    [ExpectedDays] int NOT NULL,
    [Note] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [Outcome] nvarchar(200) NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [ClosedAt] datetimeoffset NULL,
    CONSTRAINT [PK_CustodyRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FosterVolunteers] (
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [HomeLat] float NOT NULL,
    [HomeLng] float NOT NULL,
    [AcceptedSpeciesCsv] nvarchar(120) NOT NULL,
    [SizePreference] nvarchar(20) NULL,
    [MaxDays] int NOT NULL,
    [IsAvailable] bit NOT NULL,
    [AvailableUntil] datetimeoffset NULL,
    [TotalFostersCompleted] int NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FosterVolunteers] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [FoundPetReports] (
    [Id] uniqueidentifier NOT NULL,
    [FoundSpecies] int NOT NULL,
    [BreedEstimate] nvarchar(100) NULL,
    [ColorDescription] nvarchar(200) NULL,
    [SizeEstimate] nvarchar(50) NULL,
    [FoundLat] float NOT NULL,
    [FoundLng] float NOT NULL,
    [PhotoUrl] nvarchar(2048) NULL,
    [ContactName] nvarchar(100) NOT NULL,
    [ContactPhone] nvarchar(30) NOT NULL,
    [Note] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [MatchedLostPetEventId] uniqueidentifier NULL,
    [MatchScore] int NULL,
    [ReportedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FoundPetReports] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FraudReports] (
    [Id] uniqueidentifier NOT NULL,
    [ReporterUserId] uniqueidentifier NULL,
    [ReporterIpHash] nvarchar(64) NOT NULL,
    [Context] nvarchar(30) NOT NULL,
    [RelatedEntityId] uniqueidentifier NULL,
    [TargetUserId] uniqueidentifier NULL,
    [Description] nvarchar(500) NULL,
    [ReportedAt] datetimeoffset NOT NULL,
    [SuspicionLevel] int NOT NULL,
    CONSTRAINT [PK_FraudReports] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GeofencedAlertLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NOT NULL,
    [SentAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_GeofencedAlertLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [HandoverCodes] (
    [Id] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NOT NULL,
    [Code] nvarchar(4) NOT NULL,
    [GeneratedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsUsed] bit NOT NULL,
    [UsedAt] datetimeoffset NULL,
    [VerifiedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_HandoverCodes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LostPetEvents] (
    [Id] uniqueidentifier NOT NULL,
    [PetId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [LastSeenLat] float NULL,
    [LastSeenLng] float NULL,
    [LastSeenAt] datetimeoffset NOT NULL,
    [ReportedAt] datetimeoffset NOT NULL,
    [ResolvedAt] datetimeoffset NULL,
    [ReunionLat] float NULL,
    [ReunionLng] float NULL,
    [RecoveryDistanceMeters] float NULL,
    [RecoveryTime] time NULL,
    [CantonName] nvarchar(120) NULL,
    [RecentPhotoUrl] nvarchar(2000) NULL,
    [PublicMessage] nvarchar(200) NULL,
    [ContactName] nvarchar(100) NULL,
    [ContactPhone] nvarchar(30) NULL,
    [RewardAmount] decimal(12,2) NULL,
    [RewardNote] nvarchar(150) NULL,
    CONSTRAINT [PK_LostPetEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Body] nvarchar(1000) NOT NULL,
    [IsRead] bit NOT NULL,
    [RelatedEntityId] nvarchar(36) NULL,
    [ActionConfirmedAt] datetimeoffset NULL,
    [ActionSummary] nvarchar(280) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PetPhotoEmbeddings] (
    [PetId] uniqueidentifier NOT NULL,
    [EmbeddingJson] nvarchar(max) NOT NULL,
    [PhotoUrlHash] nvarchar(64) NOT NULL,
    [GeneratedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PetPhotoEmbeddings] PRIMARY KEY ([PetId])
);
GO


CREATE TABLE [Pets] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Species] nvarchar(20) NOT NULL,
    [Breed] nvarchar(100) NULL,
    [BirthDate] date NULL,
    [PhotoUrl] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [MicrochipId] nvarchar(15) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Pets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PushSubscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Endpoint] nvarchar(2048) NOT NULL,
    [KeysJson] nvarchar(512) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PushSubscriptions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [QrScanEvents] (
    [Id] uniqueidentifier NOT NULL,
    [PetId] uniqueidentifier NOT NULL,
    [ScannedByUserId] nvarchar(64) NULL,
    [IpAddress] nvarchar(64) NULL,
    [CountryCode] nvarchar(8) NULL,
    [CityName] nvarchar(120) NULL,
    [UserAgent] nvarchar(512) NULL,
    [ScannedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_QrScanEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RiskCalendarEvents] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Month] int NOT NULL,
    [Day] int NOT NULL,
    [DaysBeforeAlert] int NOT NULL,
    [MessageTemplate] nvarchar(300) NOT NULL,
    [CantonFilter] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_RiskCalendarEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SearchZones] (
    [Id] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NOT NULL,
    [Label] nvarchar(100) NOT NULL,
    [GeoJsonPolygon] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [AssignedToUserId] uniqueidentifier NULL,
    [TakenAt] datetimeoffset NULL,
    [ClearedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_SearchZones] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Sightings] (
    [Id] uniqueidentifier NOT NULL,
    [PetId] uniqueidentifier NOT NULL,
    [LostPetEventId] uniqueidentifier NULL,
    [Lat] float NOT NULL,
    [Lng] float NOT NULL,
    [PhotoUrl] nvarchar(2048) NULL,
    [Note] nvarchar(2000) NULL,
    [SightedAt] datetimeoffset NOT NULL,
    [ReportedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Sightings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [UserLocations] (
    [UserId] uniqueidentifier NOT NULL,
    [Lat] float NOT NULL,
    [Lng] float NOT NULL,
    [ReceiveNearbyAlerts] bit NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    [QuietHoursStart] time(0) NULL,
    [QuietHoursEnd] time(0) NULL,
    CONSTRAINT [PK_UserLocations] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [UserNotificationPreferences] (
    [UserId] uniqueidentifier NOT NULL,
    [EnablePreventiveAlerts] bit NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserNotificationPreferences] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [PasswordHash] nvarchar(100) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [IsEmailVerified] bit NOT NULL,
    [EmailVerificationToken] nvarchar(64) NULL,
    [EmailVerificationTokenExpiry] datetimeoffset NULL,
    [PasswordResetToken] nvarchar(64) NULL,
    [PasswordResetTokenExpiry] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [FailedLoginAttempts] int NOT NULL DEFAULT 0,
    [LockoutEnd] datetimeoffset NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ChatMessages] (
    [Id] uniqueidentifier NOT NULL,
    [ThreadId] uniqueidentifier NOT NULL,
    [SenderUserId] uniqueidentifier NOT NULL,
    [Body] nvarchar(800) NOT NULL,
    [SentAt] datetimeoffset NOT NULL,
    [IsReadByRecipient] bit NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_ChatThreads_ThreadId] FOREIGN KEY ([ThreadId]) REFERENCES [ChatThreads] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsRevoked] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [SessionIssuedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_AllyProfiles_VerificationStatus] ON [AllyProfiles] ([VerificationStatus]);
GO


CREATE INDEX [IX_BotSessions_ExpiresAt] ON [BotSessions] ([ExpiresAt]);
GO


CREATE INDEX [IX_BotSessions_PhoneNumberHash_Step] ON [BotSessions] ([PhoneNumberHash], [Step]);
GO


CREATE INDEX [IX_BroadcastAttempts_LostPetEventId] ON [BroadcastAttempts] ([LostPetEventId]);
GO


CREATE INDEX [IX_BroadcastAttempts_LostPetEventId_Channel] ON [BroadcastAttempts] ([LostPetEventId], [Channel]);
GO


CREATE INDEX [IX_ChatMessages_ThreadId] ON [ChatMessages] ([ThreadId]);
GO


CREATE INDEX [IX_ChatMessages_ThreadId_SentAt] ON [ChatMessages] ([ThreadId], [SentAt]);
GO


CREATE INDEX [IX_ChatThreads_LostPetEventId_InitiatorUserId] ON [ChatThreads] ([LostPetEventId], [InitiatorUserId]);
GO


CREATE INDEX [IX_ChatThreads_OwnerUserId] ON [ChatThreads] ([OwnerUserId]);
GO


CREATE UNIQUE INDEX [IX_Clinics_LicenseNumber] ON [Clinics] ([LicenseNumber]);
GO


CREATE INDEX [IX_Clinics_Status] ON [Clinics] ([Status]);
GO


CREATE UNIQUE INDEX [IX_Clinics_UserId] ON [Clinics] ([UserId]);
GO


CREATE INDEX [IX_ClinicScans_ClinicId] ON [ClinicScans] ([ClinicId]);
GO


CREATE INDEX [IX_ClinicScans_ClinicId_ScannedAt] ON [ClinicScans] ([ClinicId], [ScannedAt]);
GO


CREATE INDEX [IX_ContributorScores_ReunificationCount] ON [ContributorScores] ([ReunificationCount]);
GO


CREATE INDEX [IX_CustodyRecords_FosterUserId] ON [CustodyRecords] ([FosterUserId]);
GO


CREATE INDEX [IX_CustodyRecords_FoundPetReportId] ON [CustodyRecords] ([FoundPetReportId]);
GO


CREATE INDEX [IX_CustodyRecords_Status] ON [CustodyRecords] ([Status]);
GO


CREATE INDEX [IX_FosterVolunteers_HomeLatLng] ON [FosterVolunteers] ([HomeLat], [HomeLng]);
GO


CREATE INDEX [IX_FosterVolunteers_IsAvailable] ON [FosterVolunteers] ([IsAvailable]);
GO


CREATE INDEX [IX_FoundPetReports_LatLng] ON [FoundPetReports] ([FoundLat], [FoundLng]);
GO


CREATE INDEX [IX_FoundPetReports_ReportedAt] ON [FoundPetReports] ([ReportedAt]);
GO


CREATE INDEX [IX_FoundPetReports_Status] ON [FoundPetReports] ([Status]);
GO


CREATE INDEX [IX_FraudReports_ReporterIpHash_ReportedAt] ON [FraudReports] ([ReporterIpHash], [ReportedAt]);
GO


CREATE INDEX [IX_FraudReports_TargetUserId_ReportedAt] ON [FraudReports] ([TargetUserId], [ReportedAt]);
GO


CREATE INDEX [IX_GeofencedAlertLogs_UserId_LostPetEventId] ON [GeofencedAlertLogs] ([UserId], [LostPetEventId]);
GO


CREATE INDEX [IX_HandoverCodes_LostPetEventId] ON [HandoverCodes] ([LostPetEventId]);
GO


CREATE INDEX [IX_LostPetEvents_CantonName] ON [LostPetEvents] ([CantonName]);
GO


CREATE INDEX [IX_LostPetEvents_OwnerId] ON [LostPetEvents] ([OwnerId]);
GO


CREATE INDEX [IX_LostPetEvents_PetId] ON [LostPetEvents] ([PetId]);
GO


CREATE INDEX [IX_LostPetEvents_Status] ON [LostPetEvents] ([Status]);
GO


CREATE INDEX [IX_Notifications_UserId_CreatedAt] ON [Notifications] ([UserId], [CreatedAt]);
GO


CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
GO


CREATE UNIQUE INDEX [IX_Pets_MicrochipId] ON [Pets] ([MicrochipId]) WHERE [MicrochipId] IS NOT NULL;
GO


CREATE INDEX [IX_Pets_OwnerId] ON [Pets] ([OwnerId]);
GO


CREATE UNIQUE INDEX [IX_PushSubscriptions_Endpoint] ON [PushSubscriptions] ([Endpoint]);
GO


CREATE INDEX [IX_PushSubscriptions_UserId] ON [PushSubscriptions] ([UserId]);
GO


CREATE INDEX [IX_QrScanEvents_PetId_ScannedAt] ON [QrScanEvents] ([PetId], [ScannedAt]);
GO


CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
GO


CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO


CREATE INDEX [IX_RiskCalendarEvents_IsActive] ON [RiskCalendarEvents] ([IsActive]);
GO


CREATE INDEX [IX_RiskCalendarEvents_MonthDay] ON [RiskCalendarEvents] ([Month], [Day]);
GO


CREATE INDEX [IX_SearchZones_LostPetEventId] ON [SearchZones] ([LostPetEventId]);
GO


CREATE INDEX [IX_SearchZones_LostPetEventId_Status] ON [SearchZones] ([LostPetEventId], [Status]);
GO


CREATE INDEX [IX_Sightings_LatLng] ON [Sightings] ([Lat], [Lng]);
GO


CREATE INDEX [IX_Sightings_LostPetEventId] ON [Sightings] ([LostPetEventId]);
GO


CREATE INDEX [IX_Sightings_PetId] ON [Sightings] ([PetId]);
GO


CREATE INDEX [IX_UserLocations_Alerts_Lat_Lng] ON [UserLocations] ([ReceiveNearbyAlerts], [Lat], [Lng]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [IX_Users_EmailVerificationToken] ON [Users] ([EmailVerificationToken]) WHERE [EmailVerificationToken] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Users_PasswordResetToken] ON [Users] ([PasswordResetToken]) WHERE [PasswordResetToken] IS NOT NULL;
GO



