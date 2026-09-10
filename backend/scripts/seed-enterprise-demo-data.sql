-- PawTrack CR - supplemental enterprise demo data
-- Local development only: (localdb)\MSSQLLocalDB / PawTrackDev
-- Prerequisite: seed-test-users.sql and seed-extended-test-users.sql.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @adminId uniqueidentifier = 'AA000001-0000-0000-0000-000000000001';
DECLARE @reviewerId uniqueidentifier = 'DAD661E5-7B58-4A5A-ABD4-280ACA9B7C72';
DECLARE @freeOwnerId uniqueidentifier = 'AA000002-0000-0000-0000-000000000002';
DECLARE @plusOwnerId uniqueidentifier = 'AA000003-0000-0000-0000-000000000003';
DECLARE @plusPetId uniqueidentifier = 'CC000001-0000-0000-0000-000000000001';
DECLARE @freePetId uniqueidentifier = 'CC000002-0000-0000-0000-000000000002';
DECLARE @activeCollarId uniqueidentifier = 'CC100001-0000-0000-0000-000000000001';
DECLARE @offlineCollarId uniqueidentifier = 'CC100002-0000-0000-0000-000000000002';

DELETE FROM dbo.CollarSafeZones WHERE CollarId IN (@activeCollarId, @offlineCollarId);
DELETE FROM dbo.CollarLocations WHERE CollarId IN (@activeCollarId, @offlineCollarId);
DELETE FROM dbo.CollarDeviceCredentials WHERE CollarId IN (@activeCollarId, @offlineCollarId);
DELETE FROM dbo.CollarTags WHERE CollarId IN (@activeCollarId, @offlineCollarId)
   OR Serial IN ('PT-A3F9-0001234', 'PT-B4E1-0001235', 'PT-C7D2-0001236');
DELETE FROM dbo.Collars WHERE Id IN (@activeCollarId, @offlineCollarId);
DELETE FROM dbo.Pets WHERE Id IN (@plusPetId, @freePetId);
DELETE FROM dbo.BillboardDeliveryEvents WHERE BillboardId IN (
    'CC200001-0000-0000-0000-000000000001', 'CC200002-0000-0000-0000-000000000002',
    'CC200003-0000-0000-0000-000000000003', 'CC200004-0000-0000-0000-000000000004');
DELETE FROM dbo.Billboards WHERE Id IN (
    'CC200001-0000-0000-0000-000000000001', 'CC200002-0000-0000-0000-000000000002',
    'CC200003-0000-0000-0000-000000000003', 'CC200004-0000-0000-0000-000000000004');

INSERT INTO dbo.Pets (
    Id, OwnerId, Name, Species, Breed, BirthDate, Status, MicrochipId, CreatedAt,
    UpdatedAt, Color, DistinctiveMarks, MicrochipVerificationStatus, ResidenceCanton,
    ResponsibleOwnerId, Sex, SterilizedStatus)
VALUES
    (@plusPetId, @plusOwnerId, 'Toby GPS', 'Dog', 'Beagle', '2022-04-18', 'Active',
     '985141000200001', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 'Tricolor',
     'Collar azul con placa QR', 'Declared', 'Heredia', @plusOwnerId, 'Male', 'Yes'),
    (@freePetId, @freeOwnerId, 'Lola Libre', 'Cat', 'Criollo', '2023-08-02', 'Active',
     '985141000200002', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 'Negro',
     'Mancha blanca en el pecho', 'Declared', 'San Jose', @freeOwnerId, 'Female', 'Unknown');

INSERT INTO dbo.Collars (
    Id, PetId, OwnerId, Provider, ExternalDeviceId, BatteryPercent, LastLat, LastLng,
    LastSeenAt, IsActive, RegisteredAt, CollarTagSerial, OfflineAlertsEnabled,
    OfflineThresholdMinutes, IsOffline, BatteryAlertsEnabled, BatteryAlertThresholdPercent,
    IsLost, LostModeActivatedAt, LostPetEventId)
VALUES
    (@activeCollarId, @plusPetId, @plusOwnerId, 'Own', 'PT-A3F9-0001234', 76, 9.99820, -84.11670,
     DATEADD(MINUTE, -4, SYSDATETIMEOFFSET()), 1, DATEADD(DAY, -30, SYSDATETIMEOFFSET()),
     'PT-A3F9-0001234', 1, 120, 0, 1, 20, 0, NULL, NULL),
    (@offlineCollarId, @plusPetId, @plusOwnerId, 'Generic', 'demo-offline-01', 12, 9.99910, -84.11820,
     DATEADD(HOUR, -5, SYSDATETIMEOFFSET()), 1, DATEADD(DAY, -20, SYSDATETIMEOFFSET()),
     NULL, 1, 60, 1, 1, 20, 0, NULL, NULL);

INSERT INTO dbo.CollarTags (Id, Serial, CollarId, Status, FirmwareVersion, ManufacturedAt, SoldAt, ActivatedAt, LastPingAt)
VALUES
    ('CC110001-0000-0000-0000-000000000001', 'PT-A3F9-0001234', @activeCollarId, 'Activated', '1.2.0', DATEADD(DAY, -90, SYSDATETIMEOFFSET()), DATEADD(DAY, -30, SYSDATETIMEOFFSET()), DATEADD(DAY, -30, SYSDATETIMEOFFSET()), DATEADD(MINUTE, -4, SYSDATETIMEOFFSET())),
    ('CC110002-0000-0000-0000-000000000002', 'PT-B4E1-0001235', NULL, 'Unactivated', '1.2.0', DATEADD(DAY, -7, SYSDATETIMEOFFSET()), NULL, NULL, NULL),
    ('CC110003-0000-0000-0000-000000000003', 'PT-C7D2-0001236', NULL, 'Unactivated', '1.2.0', DATEADD(DAY, -2, SYSDATETIMEOFFSET()), NULL, NULL, NULL);

INSERT INTO dbo.CollarDeviceCredentials (Id, CollarId, KeyHash, CreatedAt, RevokedAt, LastUsedAt)
VALUES ('CC120001-0000-0000-0000-000000000001', @activeCollarId,
    CONVERT(varchar(64), HASHBYTES('SHA2_256', 'demo-collar-device-key'), 2),
    DATEADD(DAY, -30, SYSDATETIMEOFFSET()), NULL, DATEADD(MINUTE, -4, SYSDATETIMEOFFSET()));

INSERT INTO dbo.CollarLocations (Id, CollarId, Lat, Lng, Accuracy, RecordedAt)
VALUES
    ('CC130001-0000-0000-0000-000000000001', @activeCollarId, 9.99780, -84.11730, 8, DATEADD(HOUR, -2, SYSDATETIMEOFFSET())),
    ('CC130002-0000-0000-0000-000000000002', @activeCollarId, 9.99800, -84.11710, 7, DATEADD(HOUR, -1, SYSDATETIMEOFFSET())),
    ('CC130003-0000-0000-0000-000000000003', @activeCollarId, 9.99810, -84.11690, 6, DATEADD(MINUTE, -30, SYSDATETIMEOFFSET())),
    ('CC130004-0000-0000-0000-000000000004', @activeCollarId, 9.99820, -84.11670, 6, DATEADD(MINUTE, -4, SYSDATETIMEOFFSET()));

INSERT INTO dbo.CollarSafeZones (Id, CollarId, Name, PolygonJson, Enabled, CreatedAt, LastKnownInside)
VALUES ('CC140001-0000-0000-0000-000000000001', @activeCollarId, 'Casa Heredia',
    '[{"lat":9.9975,"lng":-84.1175},{"lat":9.9975,"lng":-84.1162},{"lat":9.9988,"lng":-84.1162},{"lat":9.9988,"lng":-84.1175}]',
    1, DATEADD(DAY, -20, SYSDATETIMEOFFSET()), 1);

INSERT INTO dbo.Billboards (
    Id, OwnerId, Title, Body, ImageUrl, CtaLabel, CtaUrl, Placement, Status, StartsAt,
    EndsAt, Priority, AdvertiserName, Category, TargetCanton, ContractReference, BudgetCrc,
    FrequencyCapPerDay, IsCategoryExclusive, IsVip, CampaignStatus, ReviewedByUserId, ReviewedAt,
    ReviewNote, CreatedAt, UpdatedAt)
VALUES
    ('CC200001-0000-0000-0000-000000000001', @adminId, 'GPS para proteger cada paseo',
     'Monitorea a tu mascota con ubicación en tiempo real.', 'https://placehold.co/1200x628/png?text=GPS+PawTrack',
     'Conocer GPS', 'https://pawtrack.cr', 0, 1, DATEADD(DAY, -1, SYSDATETIMEOFFSET()), DATEADD(DAY, 30, SYSDATETIMEOFFSET()),
    90, 'PawTrack GPS', 'GpsAndIdentification', 'Heredia', 'DEMO-VIP-001', 100000, 2, 0, 1, 'Approved', @reviewerId,
     DATEADD(DAY, -2, SYSDATETIMEOFFSET()), 'Demo VIP aprobada', DATEADD(DAY, -3, SYSDATETIMEOFFSET()), DATEADD(DAY, -2, SYSDATETIMEOFFSET())),
    ('CC200002-0000-0000-0000-000000000002', @adminId, 'Apoyo en búsqueda urgente',
     'Servicios de recuperación y orientación veterinaria.', 'https://placehold.co/1200x628/png?text=Recuperacion',
     'Ver ayuda', 'https://pawtrack.cr', 3, 1, DATEADD(DAY, -1, SYSDATETIMEOFFSET()), DATEADD(DAY, 14, SYSDATETIMEOFFSET()),
    50, 'Red de Recuperación CR', 'RecoveryService', 'San Jose', 'DEMO-REC-001', 50000, 1, 0, 0, 'Approved', @reviewerId,
        DATEADD(DAY, -2, SYSDATETIMEOFFSET()), 'Categoría permitida', DATEADD(DAY, -3, SYSDATETIMEOFFSET()), DATEADD(DAY, -2, SYSDATETIMEOFFSET())),
        ('CC200003-0000-0000-0000-000000000003', @adminId, 'Protege cada paseo',
        'Activa herramientas para cuidar mejor la identidad y seguridad de tus mascotas.', NULL,
        'Ver planes', '/perfil', 1, 1, DATEADD(DAY, -1, SYSDATETIMEOFFSET()), DATEADD(DAY, 30, SYSDATETIMEOFFSET()),
        30, 'PawTrack CR', 'GpsAndIdentification', NULL, 'DEMO-DASH-001', 25000, 2, 0, 0, 'Approved', @reviewerId,
        DATEADD(DAY, -2, SYSDATETIMEOFFSET()), 'Campaña demo dashboard', DATEADD(DAY, -3, SYSDATETIMEOFFSET()), DATEADD(DAY, -2, SYSDATETIMEOFFSET())),
        ('CC200004-0000-0000-0000-000000000004', @adminId, 'Encuentra apoyo para tu mascota',
        'Conoce servicios de confianza para su cuidado, bienestar y entrenamiento.', NULL,
        'Explorar servicios', '/servicios', 9, 1, DATEADD(DAY, -1, SYSDATETIMEOFFSET()), DATEADD(DAY, 30, SYSDATETIMEOFFSET()),
        30, 'PawTrack CR', 'PetCare', NULL, 'DEMO-SERV-001', 25000, 2, 0, 0, 'Approved', @reviewerId,
        DATEADD(DAY, -2, SYSDATETIMEOFFSET()), 'Campaña demo servicios', DATEADD(DAY, -3, SYSDATETIMEOFFSET()), DATEADD(DAY, -2, SYSDATETIMEOFFSET()));

COMMIT;

SELECT 'Pets' AS EntityName, COUNT(*) AS Seeded FROM dbo.Pets WHERE Id IN (@plusPetId, @freePetId)
UNION ALL SELECT 'Collars', COUNT(*) FROM dbo.Collars WHERE Id IN (@activeCollarId, @offlineCollarId)
UNION ALL SELECT 'CollarTags', COUNT(*) FROM dbo.CollarTags WHERE Serial IN ('PT-A3F9-0001234', 'PT-B4E1-0001235', 'PT-C7D2-0001236')
UNION ALL SELECT 'CollarLocations', COUNT(*) FROM dbo.CollarLocations WHERE CollarId = @activeCollarId
UNION ALL SELECT 'CollarSafeZones', COUNT(*) FROM dbo.CollarSafeZones WHERE CollarId = @activeCollarId
UNION ALL SELECT 'Billboards', COUNT(*) FROM dbo.Billboards WHERE Id IN (
    'CC200001-0000-0000-0000-000000000001', 'CC200002-0000-0000-0000-000000000002',
    'CC200003-0000-0000-0000-000000000003', 'CC200004-0000-0000-0000-000000000004');