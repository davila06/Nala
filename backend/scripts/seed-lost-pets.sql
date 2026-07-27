-- ============================================================
-- PawTrack CR — Lost Pets Seed Script
-- Generated: 2026-07-24
--
-- Inserts test Pets + active LostPetEvents with GAM coordinates
-- so the /map page shows real markers during local development.
--
-- Requires: seed-test-users.sql must have run first
--           (owner user D73FC5EA must exist)
--
-- Run against the LOCAL dev database only.
-- NEVER run in staging or production.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

DECLARE @OwnerId UNIQUEIDENTIFIER = 'D73FC5EA-6F8F-4ADF-9756-07480962EAF3';

-- ── Idempotent cleanup ────────────────────────────────────────────────────────
DELETE FROM [dbo].[LostPetEvents]
WHERE [Id] IN (
    'A1000000-0000-0000-0000-000000000001',
    'A1000000-0000-0000-0000-000000000002',
    'A1000000-0000-0000-0000-000000000003',
    'A1000000-0000-0000-0000-000000000004',
    'A1000000-0000-0000-0000-000000000005'
);

DELETE FROM [dbo].[Pets]
WHERE [Id] IN (
    'B1000000-0000-0000-0000-000000000001',
    'B1000000-0000-0000-0000-000000000002',
    'B1000000-0000-0000-0000-000000000003',
    'B1000000-0000-0000-0000-000000000004',
    'B1000000-0000-0000-0000-000000000005'
);

-- ── Pets ──────────────────────────────────────────────────────────────────────
INSERT INTO [dbo].[Pets] ([Id], [OwnerId], [Name], [Species], [Breed], [BirthDate], [PhotoUrl], [Status], [MicrochipId], [CreatedAt], [UpdatedAt])
VALUES
    ('B1000000-0000-0000-0000-000000000001', @OwnerId, 'Max',   'Dog', 'Labrador',      '2021-03-15', NULL, 'Lost', NULL, GETUTCDATE(), GETUTCDATE()),
    ('B1000000-0000-0000-0000-000000000002', @OwnerId, 'Luna',  'Cat', 'Siamés',        '2020-07-22', NULL, 'Lost', NULL, GETUTCDATE(), GETUTCDATE()),
    ('B1000000-0000-0000-0000-000000000003', @OwnerId, 'Coco',  'Dog', 'Chihuahua',     '2022-01-10', NULL, 'Lost', NULL, GETUTCDATE(), GETUTCDATE()),
    ('B1000000-0000-0000-0000-000000000004', @OwnerId, 'Michi', 'Cat', NULL,            '2019-11-05', NULL, 'Lost', NULL, GETUTCDATE(), GETUTCDATE()),
    ('B1000000-0000-0000-0000-000000000005', @OwnerId, 'Rocky', 'Dog', 'Pastor Alemán', '2020-05-18', NULL, 'Lost', NULL, GETUTCDATE(), GETUTCDATE());

-- ── LostPetEvents — active, spread across the GAM ────────────────────────────
-- Columns that can be NULL are omitted; Status stored as string per EF config.
INSERT INTO [dbo].[LostPetEvents] (
    [Id], [PetId], [OwnerId],
    [Status], [Description], [PublicMessage],
    [LastSeenLat], [LastSeenLng], [LastSeenAt], [ReportedAt],
    [ContactName], [ContactPhone],
    [RewardAmount], [RewardNote], [CantonName],
    [RecentPhotoUrl], [ResolvedAt],
    [ReunionLat], [ReunionLng], [RecoveryDistanceMeters], [RecoveryTime]
)
VALUES
    -- Max — San José Centro
    ('A1000000-0000-0000-0000-000000000001',
     'B1000000-0000-0000-0000-000000000001', @OwnerId,
     'Active', 'Labrador negro, collar rojo. Escapó por el portón del garaje.',
     'Si lo ves llama de inmediato, es muy amigable.',
     9.9317, -84.0828,
     DATEADD(HOUR, -6, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE()),
     'Ana Pérez', '88001234',
     25000, 'Recompensa a quien lo encuentre', 'San José',
     NULL, NULL, NULL, NULL, NULL, NULL),

    -- Luna — Cartago
    ('A1000000-0000-0000-0000-000000000002',
     'B1000000-0000-0000-0000-000000000002', @OwnerId,
     'Active', 'Gata siamesa, ojos azules, muy tímida. Desapareció cerca del parque.',
     'Por favor no la asuste, se asusta fácil.',
     9.8631, -83.9205,
     DATEADD(HOUR, -24, GETUTCDATE()), DATEADD(HOUR, -23, GETUTCDATE()),
     'Ana Pérez', '88001234',
     15000, NULL, 'Cartago',
     NULL, NULL, NULL, NULL, NULL, NULL),

    -- Coco — Heredia
    ('A1000000-0000-0000-0000-000000000003',
     'B1000000-0000-0000-0000-000000000003', @OwnerId,
     'Active', 'Chihuahua café, muy pequeño. Lleva collar azul sin placa.',
     '¡Ayúdanos a encontrarlo! Es muy querido.',
     9.9994, -84.1168,
     DATEADD(HOUR, -12, GETUTCDATE()), DATEADD(HOUR, -11, GETUTCDATE()),
     'Ana Pérez', '88001234',
     NULL, NULL, 'Heredia',
     NULL, NULL, NULL, NULL, NULL, NULL),

    -- Michi — Alajuela
    ('A1000000-0000-0000-0000-000000000004',
     'B1000000-0000-0000-0000-000000000004', @OwnerId,
     'Active', 'Gato naranja, castrado. Vive en la casa pero salió y no volvió.',
     NULL,
     10.0158, -84.2136,
     DATEADD(HOUR, -48, GETUTCDATE()), DATEADD(HOUR, -47, GETUTCDATE()),
     'Ana Pérez', '88001234',
     NULL, NULL, 'Alajuela',
     NULL, NULL, NULL, NULL, NULL, NULL),

    -- Rocky — Escazú
    ('A1000000-0000-0000-0000-000000000005',
     'B1000000-0000-0000-0000-000000000005', @OwnerId,
     'Active', 'Pastor Alemán grande, pelo corto. Se asustó con los cohetes y huyó.',
     'Si lo ven no intenten agarrarlo, puede estar asustado. Avísenme y yo voy.',
     9.9181, -84.1462,
     DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()),
     'Ana Pérez', '88001234',
     50000, 'Recompensa de ₡50 000', 'Escazú',
     NULL, NULL, NULL, NULL, NULL, NULL);

COMMIT TRANSACTION;

PRINT 'Seed completado: 5 mascotas perdidas insertadas en el GAM.';
