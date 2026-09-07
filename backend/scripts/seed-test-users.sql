-- ============================================================
-- PawTrack CR — Test Users Seed Script
-- Generated: 2026-04-08
--
-- Inserts one verified user for each UserRole.
-- All accounts: IsEmailVerified = 1, no lockout, no tokens.
-- Passwords hashed with BCrypt.Net-Next cost factor 12.
--
-- Run against the LOCAL dev database only.
-- NEVER run in staging or production.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

-- ── Idempotent: remove existing test seed rows ────────────────────────────
DELETE FROM [dbo].[ServiceAvailabilityRules]
WHERE [ProviderServiceId] = '116079AB-3431-4246-B538-3B123B293B74';
DELETE FROM [dbo].[ProviderServices]
WHERE [Id] = '116079AB-3431-4246-B538-3B123B293B74';
DELETE FROM [dbo].[ServiceProviders]
WHERE [Id] = 'C81A79F1-6A56-4BD5-BE64-328E978786AA'
   OR [UserId] = 'B4A3A5D3-08F5-45CE-91F4-EC013463A7D8';
DELETE FROM [dbo].[Users]
WHERE [Email] IN (
    'owner@pawtrack.test',
    'ally@pawtrack.test',
    'admin@pawtrack.test',
    'clinic@pawtrack.test',
    'provider@pawtrack.test'
);

-- ── Owner ─────────────────────────────────────────────────────────────────
-- Password: Test123!
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
) VALUES (
    'D73FC5EA-6F8F-4ADF-9756-07480962EAF3',
    'owner@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    N'Ana P' + NCHAR(233) + N'rez (Owner)',
    'Owner',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
);

-- ── Service provider ─────────────────────────────────────────────────────
-- Password: Test123!
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
) VALUES (
    'B4A3A5D3-08F5-45CE-91F4-EC013463A7D8',
    'provider@pawtrack.test',
    '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES',
    'Grooming E2E',
    'ServiceProvider',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
);

-- ── Ally ──────────────────────────────────────────────────────────────────
-- Password: Ally123!
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
) VALUES (
    'E2984533-3A78-4C84-8286-7C91E69AE1B3',
    'ally@pawtrack.test',
    '$2a$12$1BbkfBTccZR17WAmeXQoK.it6ig5SSyAk1hoPD3kl.YQG/u8lul7i',
    'Carlos Mora (Ally)',
    'Ally',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
);

-- ── Admin ─────────────────────────────────────────────────────────────────
-- Password: Admin123!
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
) VALUES (
    'DAD661E5-7B58-4A5A-ABD4-280ACA9B7C72',
    'admin@pawtrack.test',
    '$2a$12$sapENPrv3mwah7zOBZvjfOIZK8PoV0h2YIt11KW4u6l1F3Ah2Mis6',
    'Denis Admin',
    'Admin',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
);

-- ── Clinic ────────────────────────────────────────────────────────────────
-- Password: Clinic123!
INSERT INTO [dbo].[Users] (
    [Id], [Email], [PasswordHash], [Name], [Role],
    [IsEmailVerified], [EmailVerificationToken], [EmailVerificationTokenExpiry],
    [PasswordResetToken], [PasswordResetTokenExpiry],
    [FailedLoginAttempts], [LockoutEnd], [CreatedAt]
) VALUES (
    '2B9B9F17-39DD-42A7-B138-A00632ABE55A',
    'clinic@pawtrack.test',
    '$2a$12$Zc2bzcB9S7nfYbiB8Gq6QePRRlhy6tVgs1jeRm2SejAhwxWn3IY3W',
    N'Cl' + NCHAR(237) + N'nica VetCare CR',
    'Clinic',
    1, NULL, NULL, NULL, NULL,
    0, NULL, GETUTCDATE()
);

COMMIT;

-- ── Seed Clinics table for the Clinic test user ───────────────────────────
-- The Clinics table has a 1-to-1 relationship with Users (UserId unique).
-- Without this row, GET /api/clinics/me returns 404.

DELETE FROM [dbo].[Clinics]
WHERE [UserId] = '2B9B9F17-39DD-42A7-B138-A00632ABE55A';

INSERT INTO [dbo].[Clinics] (
    [Id], [UserId], [Name], [LicenseNumber], [Address],
    [Lat], [Lng], [ContactEmail], [Status], [RegisteredAt]
) VALUES (
    NEWID(),
    '2B9B9F17-39DD-42A7-B138-A00632ABE55A',
    N'Cl' + NCHAR(237) + N'nica VetCare CR',
    'VET-2024-TEST',
    'San José, Costa Rica',
    9.928100,
    -84.090800,
    'clinic@pawtrack.test',
    'Active',
    GETUTCDATE()
);

-- ── Service provider profile + published service for E2E ──────────────────
INSERT INTO [dbo].[ServiceProviders] (
    [Id], [UserId], [Name], [Description], [Category], [Address], [Lat], [Lng],
    [ContactEmail], [IsFeatured], [Status], [RegisteredAt]
) VALUES (
    'C81A79F1-6A56-4BD5-BE64-328E978786AA',
    'B4A3A5D3-08F5-45CE-91F4-EC013463A7D8',
    'Grooming E2E', 'Servicio de grooming para pruebas locales', 1,
    'Heredia, Costa Rica', 9.998000, -84.117000,
    'provider@pawtrack.test', 0, 1, GETUTCDATE()
);

INSERT INTO [dbo].[ProviderServices] (
    [Id], [ServiceProviderId], [Name], [Description], [Modality], [DurationMinutes],
    [PriceCrc], [Capacity], [Status], [CreatedAt]
) VALUES (
    '116079AB-3431-4246-B538-3B123B293B74',
    'C81A79F1-6A56-4BD5-BE64-328E978786AA',
    'Bano E2E', 'Servicio de prueba', 0, 60, 20000, 5, 0, GETUTCDATE()
);

-- ── Verify ────────────────────────────────────────────────────────────────
SELECT [Id], [Email], [Name], [Role], [IsEmailVerified], [CreatedAt]
FROM [dbo].[Users]
WHERE [Email] LIKE '%@pawtrack.test'
ORDER BY [CreatedAt];
