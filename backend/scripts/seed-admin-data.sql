-- ============================================================
-- seed-admin-data.sql
-- Datos de prueba para el Panel de Administración
-- Tabs: "Aliados pendientes" + "Clínicas pendientes"
--
-- Todos los usuarios nuevos usan contraseña: Test123!
-- Ejecutar DESPUÉS de seed-test-users.sql
-- ============================================================

BEGIN TRANSACTION;

-- ── 1. Ally users adicionales ─────────────────────────────────────────────

-- Ally 2: rescatista independiente
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000001',
    'maria.garcia@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'María García',
    'Ally',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Users] WHERE [Id] = 'A0000001-0000-0000-0000-000000000001'
);

-- Ally 3: ONG rescatadora
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000002',
    'patitas.felices@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'Fundación Patitas Felices',
    'Ally',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Users] WHERE [Id] = 'A0000001-0000-0000-0000-000000000002'
);

-- Ally 4: refugio animal
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000003',
    'refugio.animal.cr@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'Refugio Animal CR',
    'Ally',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Users] WHERE [Id] = 'A0000001-0000-0000-0000-000000000003'
);

-- ── 2. AllyProfiles pendientes ────────────────────────────────────────────
-- Carlos Mora (E2984533) — ya existe como usuario, le faltaba el perfil
INSERT INTO [dbo].[AllyProfiles] (
    [UserId], [OrganizationName], [AllyType], [CoverageLabel],
    [CoverageLat], [CoverageLng], [CoverageRadiusMetres],
    [VerificationStatus], [AppliedAt], [VerifiedAt]
)
SELECT
    'E2984533-3A78-4C84-8286-7C91E69AE1B3',
    'Rescate Animal Carlos Mora',
    'PetFriendlyBusiness',
    'San José Centro',
    9.9281, -84.0907, 5000,
    'Pending', DATEADD(DAY, -5, GETUTCDATE()), NULL
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AllyProfiles] WHERE [UserId] = 'E2984533-3A78-4C84-8286-7C91E69AE1B3'
);

-- María García — rescatista independiente, Heredia
INSERT INTO [dbo].[AllyProfiles] (
    [UserId], [OrganizationName], [AllyType], [CoverageLabel],
    [CoverageLat], [CoverageLng], [CoverageRadiusMetres],
    [VerificationStatus], [AppliedAt], [VerifiedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000001',
    'Red de Rescatistas Heredia',
    'Shelter',
    'Heredia y alrededores',
    9.9988, -84.1160, 8000,
    'Pending', DATEADD(DAY, -2, GETUTCDATE()), NULL
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AllyProfiles] WHERE [UserId] = 'A0000001-0000-0000-0000-000000000001'
);

-- Fundación Patitas Felices — ONG, Cartago
INSERT INTO [dbo].[AllyProfiles] (
    [UserId], [OrganizationName], [AllyType], [CoverageLabel],
    [CoverageLat], [CoverageLng], [CoverageRadiusMetres],
    [VerificationStatus], [AppliedAt], [VerifiedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000002',
    N'Fundaci' + NCHAR(243) + N'n Patitas Felices',
    'Municipality',
    N'Cartago y Gran ' + NCHAR(193) + N'rea Metropolitana',
    9.8638, -83.9200, 15000,
    'Pending', DATEADD(DAY, -10, GETUTCDATE()), NULL
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AllyProfiles] WHERE [UserId] = 'A0000001-0000-0000-0000-000000000002'
);

-- Refugio Animal CR — refugio, Alajuela
INSERT INTO [dbo].[AllyProfiles] (
    [UserId], [OrganizationName], [AllyType], [CoverageLabel],
    [CoverageLat], [CoverageLng], [CoverageRadiusMetres],
    [VerificationStatus], [AppliedAt], [VerifiedAt]
)
SELECT
    'A0000001-0000-0000-0000-000000000003',
    'Refugio Animal CR',
    'Shelter',
    'Alajuela Oeste',
    9.9928, -84.2072, 10000,
    'Pending', DATEADD(DAY, -1, GETUTCDATE()), NULL
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AllyProfiles] WHERE [UserId] = 'A0000001-0000-0000-0000-000000000003'
);

-- ── 3. Clinic users adicionales ───────────────────────────────────────────

-- Clinic 2: Animal House
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
)
SELECT
    'C0000001-0000-0000-0000-000000000001',
    'animal.house@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'Clínica Animal House',
    'Clinic',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Users] WHERE [Id] = 'C0000001-0000-0000-0000-000000000001'
);

-- Clinic 3: Veterinaria Los Ángeles
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
)
SELECT
    'C0000001-0000-0000-0000-000000000002',
    'vet.angeles@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'Veterinaria Los Ángeles',
    'Clinic',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Users] WHERE [Id] = 'C0000001-0000-0000-0000-000000000002'
);

-- ── 4. Clinics pendientes de aprobación ───────────────────────────────────

-- Clínica Animal House — San José
INSERT INTO [dbo].[Clinics] (
    [Id], [UserId], [Name], [LicenseNumber], [Address],
    [Lat], [Lng], [ContactEmail], [Status], [RegisteredAt]
)
SELECT
    'C0000002-0000-0000-0000-000000000001',
    'C0000001-0000-0000-0000-000000000001',
    'Clínica Animal House',
    'SENASA-2024-0892',
    'Av. Central, San José, 100m norte del Banco Nacional',
    9.9302, -84.0820,
    'animal.house@pawtrack.test',
    'Pending',
    DATEADD(DAY, -3, GETUTCDATE())
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Clinics] WHERE [Id] = 'C0000002-0000-0000-0000-000000000001'
);

-- Veterinaria Los Ángeles — Alajuela
INSERT INTO [dbo].[Clinics] (
    [Id], [UserId], [Name], [LicenseNumber], [Address],
    [Lat], [Lng], [ContactEmail], [Status], [RegisteredAt]
)
SELECT
    'C0000002-0000-0000-0000-000000000002',
    'C0000001-0000-0000-0000-000000000002',
    N'Veterinaria Los ' + NCHAR(193) + N'ngeles',
    'SENASA-2025-0341',
    N'Barrio Los ' + NCHAR(193) + N'ngeles, Alajuela, frente al parque',
    9.9956, -84.2164,
    'vet.angeles@pawtrack.test',
    'Pending',
    DATEADD(DAY, -7, GETUTCDATE())
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Clinics] WHERE [Id] = 'C0000002-0000-0000-0000-000000000002'
);

COMMIT;

PRINT 'seed-admin-data completado:'
PRINT '  - 4 AllyProfiles pendientes'
PRINT '  - 2 Clinics pendientes'
PRINT '  - 5 nuevos usuarios (3 Ally + 2 Clinic)'
