-- ============================================================
-- PawTrack CR — Extended Test Users Seed Script
-- Generated: 2026-08-03
--
-- Extends the base seed with users for all roles/tiers/plans
-- described in the test documentation table.
-- All passwords: Test1234! (BCrypt cost 12)
-- All accounts: IsEmailVerified = 1
--
-- Run against LOCAL dev database only.
-- Server: CPC-davil-ECEKS\SQLEXPRESS | DB: PawTrackLocal
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Clean up existing test accounts ───────────────────────────────────────
-- Remove the Familia medical demo data first so this script is idempotent.
DELETE FROM [dbo].[VetReminders]
WHERE [PetId] IN (
    'AA100001-0000-0000-0000-000000000001',
    'AA100001-0000-0000-0000-000000000002');
DELETE FROM [dbo].[ActivityLogs]
WHERE [PetId] IN (
    'AA100001-0000-0000-0000-000000000001',
    'AA100001-0000-0000-0000-000000000002');
DELETE FROM [dbo].[MedicalRecords]
WHERE [PetId] IN (
    'AA100001-0000-0000-0000-000000000001',
    'AA100001-0000-0000-0000-000000000002');
DELETE FROM [dbo].[Pets]
WHERE [Id] IN (
    'AA100001-0000-0000-0000-000000000001',
    'AA100001-0000-0000-0000-000000000002');

DELETE FROM [dbo].[Clinics]      WHERE [ContactEmail] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr');
DELETE FROM [dbo].[Stores]       WHERE [ContactEmail] = 'tienda_activa@test.cr';
DELETE FROM [dbo].[Subscriptions] WHERE [UserId] IN (
    SELECT [Id] FROM [dbo].[Users] WHERE [Email] IN (
        'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
        'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
        'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr'))
    OR [ClinicOwnerId] IN (
    SELECT [Id] FROM [dbo].[Users] WHERE [Email] IN (
        'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
        'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
        'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr'));
DELETE FROM [dbo].[Users] WHERE [Email] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr','tienda_activa@test.cr',
    'soporte_bienestar@test.cr');
GO

-- ── All new test users share password: Test1234! ──────────────────────────
-- BCrypt hash of "Test1234!" cost=12
DECLARE @hash NVARCHAR(200) = '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES';
-- NOTE: This is the hash of "Test123!" from the existing seed.
-- We reuse it because HashGen.exe is unavailable during automated seeding.
-- Change individual hashes here if you need different passwords per account.

-- ── 1. Admin ──────────────────────────────────────────────────────────────
-- admin@pawtrack.cr / Test123!
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000001-0000-0000-0000-000000000001','admin@pawtrack.cr',@hash,'Admin PawTrack','Admin',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 2. Owner — Explorador (sin suscripción paga) ──────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000002-0000-0000-0000-000000000002','owner_free@test.cr',@hash,'Ana Libre (Free)','Owner',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 3. Owner — UserPlus activo ────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000003-0000-0000-0000-000000000003','owner_plus@test.cr',@hash,'Pedro Plus','Owner',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 4. Owner — UserFamilia activo ─────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000004-0000-0000-0000-000000000004','owner_familia@test.cr',@hash,'Laura Familia','Owner',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 5. Ally ───────────────────────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000005-0000-0000-0000-000000000005','ally@test.cr',@hash,'Refugio Central CR','Ally',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 6. Clinic — ClinicBasic ───────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000006-0000-0000-0000-000000000006','clinica_basica@test.cr',@hash,'VetBasica Test','Clinic',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 7. Clinic — ClinicPartner ─────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000007-0000-0000-0000-000000000007','clinica_partner@test.cr',@hash,'VetPartner Elite','Clinic',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 8. Municipality — Básica ──────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000008-0000-0000-0000-000000000008','municipal_basica@test.cr',@hash,'Muni Desamparados','Municipality',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 9. Municipality — Full ────────────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000009-0000-0000-0000-000000000009',N'municipal_full@test.cr',@hash,N'Muni San Jos' + NCHAR(233) + N' Full','Municipality',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 10. Municipality — RedRegional ────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000010-0000-0000-0000-000000000010','municipal_regional@test.cr',@hash,'Red Regional Norte','Municipality',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 11. Store — activa (cierra el gap de rol Store sin usuario de prueba) ─
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000011-0000-0000-0000-000000000011','tienda_activa@test.cr',@hash,'PetShop CR Test','Store',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 12. Support — especialista de bienestar animal (rol solo asignable, sin perfil propio) ─
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000012-0000-0000-0000-000000000012','soporte_bienestar@test.cr',@hash,'Soporte Bienestar Animal','Support',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());
GO

-- ── Clinics table — ClinicBasic (status Active) ───────────────────────────
INSERT INTO [dbo].[Clinics] ([Id],[UserId],[Name],[LicenseNumber],[Address],[Lat],[Lng],[ContactEmail],[Status],[RegisteredAt])
VALUES (NEWID(),'AA000006-0000-0000-0000-000000000006','VetBasica Test','VET-TEST-BASIC','Alajuela, Costa Rica',10.015694,-84.214631,'clinica_basica@test.cr','Active',GETUTCDATE());

-- ── Clinics table — ClinicPartner (status Active) ────────────────────────
INSERT INTO [dbo].[Clinics] ([Id],[UserId],[Name],[LicenseNumber],[Address],[Lat],[Lng],[ContactEmail],[Status],[RegisteredAt])
VALUES (NEWID(),'AA000007-0000-0000-0000-000000000007','VetPartner Elite','VET-TEST-PARTNER',N'Escaz' + NCHAR(250) + N', San Jos' + NCHAR(233),9.921538,-84.142429,'clinica_partner@test.cr','Active',GETUTCDATE());
GO

-- ── Stores table — Store activa ───────────────────────────────────────────
INSERT INTO [dbo].[Stores] ([Id],[UserId],[Name],[Description],[Address],[Lat],[Lng],[ContactEmail],[IsFeatured],[Status],[RegisteredAt])
VALUES (NEWID(),'AA000011-0000-0000-0000-000000000011','PetShop CR Test','Tienda de prueba para el directorio de pet stores','Heredia, Costa Rica',9.998000,-84.117000,'tienda_activa@test.cr',0,1,GETUTCDATE());
GO

-- ── Subscriptions — owner_plus: UserPlus activo ───────────────────────────
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
VALUES (NEWID(),'AA000003-0000-0000-0000-000000000003',NULL,NULL,'UserPlus','Active','TESTPLUS',2990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL);

-- ── Subscriptions — owner_familia: UserFamilia activo ────────────────────
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
VALUES (NEWID(),'AA000004-0000-0000-0000-000000000004',NULL,NULL,'UserFamilia','Active','TESTFAM1',4990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL);

-- ── Pets + medical history — owner_familia ────────────────────────────────
-- Login: owner_familia@test.cr / Test123!
INSERT INTO [dbo].[Pets] (
    [Id],[OwnerId],[Name],[Species],[Breed],[BirthDate],[PhotoUrl],[Status],
    [MicrochipId],[CreatedAt],[UpdatedAt],[Color],[DistinctiveMarks],
    [MicrochipVerificationStatus],[ResidenceCanton],[ResponsibleOwnerId],
    [Sex],[SterilizedStatus],[SterilizedAt]
) VALUES
(
    'AA100001-0000-0000-0000-000000000001',
    'AA000004-0000-0000-0000-000000000004',
    'Nala Familia', 'Dog', 'Golden Retriever', '2021-05-12', NULL, 'Active',
    '985141000100001', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(),
    'Dorado', 'Mancha blanca en el pecho', 'Declared', 'Heredia',
    'AA000004-0000-0000-0000-000000000004', 'Female', 'Yes', '2022-02-20'
),
(
    'AA100001-0000-0000-0000-000000000002',
    'AA000004-0000-0000-0000-000000000004',
    'Milo Familia', 'Cat', N'Siam' + NCHAR(233) + N's', '2022-09-03', NULL, 'Active',
    '985141000100002', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(),
    'Crema', 'Ojos azules', 'Declared', 'Heredia',
    'AA000004-0000-0000-0000-000000000004', 'Male', 'Unknown', NULL
);

INSERT INTO [dbo].[MedicalRecords] (
    [Id],[PetId],[CreatedByUserId],[ClinicId],[Type],[Date],[Description],
    [VetName],[ClinicName],[NextDueDate],[DocumentUrl],[CreatedAt],
    [WeightKg],[DosageDescription],[Frequency],[DurationDays],[MedicationEndDate]
) VALUES
(
    NEWID(), 'AA100001-0000-0000-0000-000000000001',
    'AA000004-0000-0000-0000-000000000004', NULL, 0, '2026-01-15',
    'Vacuna antirrabica anual aplicada. Lote demo FAM-2026-01.',
    'Dra. Valeria Rojas', 'Clinica VetCare CR', '2027-01-15', NULL,
    SYSDATETIMEOFFSET(), 28.40, NULL, NULL, NULL, NULL
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000001',
    'AA000004-0000-0000-0000-000000000004', NULL, 1, '2025-12-10',
    'Desparasitacion interna y externa de control.',
    'Dra. Valeria Rojas', 'Clinica VetCare CR', '2026-06-10', NULL,
    SYSDATETIMEOFFSET(), 27.90, NULL, NULL, NULL, NULL
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000001',
    'AA000004-0000-0000-0000-000000000004', NULL, 2, '2026-03-02',
    'Chequeo general: signos vitales normales y peso estable.',
    'Dr. Mateo Solis', 'Hospital Veterinario Central', '2026-09-02', NULL,
    SYSDATETIMEOFFSET(), 28.70, NULL, NULL, NULL, NULL
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000001',
    'AA000004-0000-0000-0000-000000000004', NULL, 5, '2026-03-02',
    'Tratamiento de apoyo por dermatitis estacional.',
    'Dr. Mateo Solis', 'Hospital Veterinario Central', '2026-03-16', NULL,
    SYSDATETIMEOFFSET(), 28.70, '1 tableta de 10 mg', 'Cada 24 horas', 14, '2026-03-16'
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000002',
    'AA000004-0000-0000-0000-000000000004', NULL, 0, '2026-02-08',
    'Vacuna triple felina aplicada. Lote demo CAT-2026-02.',
    'Dra. Valeria Rojas', 'Clinica VetCare CR', '2027-02-08', NULL,
    SYSDATETIMEOFFSET(), 4.80, NULL, NULL, NULL, NULL
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000002',
    'AA000004-0000-0000-0000-000000000004', NULL, 2, '2026-02-08',
    'Chequeo general y control dental preventivo.',
    'Dra. Valeria Rojas', 'Clinica VetCare CR', '2026-08-08', NULL,
    SYSDATETIMEOFFSET(), 4.80, NULL, NULL, NULL, NULL
),
(
    NEWID(), 'AA100001-0000-0000-0000-000000000002',
    'AA000004-0000-0000-0000-000000000004', NULL, 6, '2026-02-08',
    'Alergia alimentaria registrada; evitar pollo en la dieta.',
    'Dra. Valeria Rojas', 'Clinica VetCare CR', NULL, NULL,
    SYSDATETIMEOFFSET(), 4.80, NULL, NULL, NULL, NULL
);

INSERT INTO [dbo].[ActivityLogs] (
    [Id],[PetId],[OwnerId],[Date],[Type],[DurationMinutes],[DistanceMeters],
    [Notes],[Source],[CreatedAt]
) VALUES
    (NEWID(),'AA100001-0000-0000-0000-000000000001','AA000004-0000-0000-0000-000000000004','2026-03-06',0,35,2800,'Paseo matutino en familia.',0,SYSDATETIMEOFFSET()),
    (NEWID(),'AA100001-0000-0000-0000-000000000001','AA000004-0000-0000-0000-000000000004','2026-03-07',4,20,NULL,'Sesion de obediencia basica.',0,SYSDATETIMEOFFSET()),
    (NEWID(),'AA100001-0000-0000-0000-000000000002','AA000004-0000-0000-0000-000000000004','2026-03-06',2,15,NULL,'Juego con enriquecimiento ambiental.',0,SYSDATETIMEOFFSET());

INSERT INTO [dbo].[VetReminders] (
    [Id],[PetId],[OwnerId],[Type],[DueDate],[Title],[Notes],[IsCompleted],
    [CompletedAt],[ReminderSentAt],[CreatedAt]
) VALUES
    (NEWID(),'AA100001-0000-0000-0000-000000000001','AA000004-0000-0000-0000-000000000004',0,'2027-01-15','Refuerzo de vacuna antirrabica','Agenda la cita con la clinica.',0,NULL,NULL,SYSDATETIMEOFFSET()),
    (NEWID(),'AA100001-0000-0000-0000-000000000001','AA000004-0000-0000-0000-000000000004',1,'2026-06-10','Desparasitacion semestral','Recordatorio de control preventivo.',0,NULL,NULL,SYSDATETIMEOFFSET()),
    (NEWID(),'AA100001-0000-0000-0000-000000000002','AA000004-0000-0000-0000-000000000004',0,'2027-02-08','Refuerzo de vacuna triple felina','Revisar cartilla de Milo.',0,NULL,NULL,SYSDATETIMEOFFSET());

-- ── Subscriptions — clinica_partner: ClinicPartner activo ────────────────
-- ClinicId is the Clinic.Id (not the UserId); get it dynamically
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
SELECT NEWID(),NULL,c.[Id],'AA000007-0000-0000-0000-000000000007','ClinicPartner','Active','TESTPART',35000,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL
FROM [dbo].[Clinics] c WHERE c.[UserId]='AA000007-0000-0000-0000-000000000007';
GO

-- ── AllyProfile — ally@test.cr (Verified) ────────────────────────────────
DELETE FROM [dbo].[AllyProfiles] WHERE [UserId]='AA000005-0000-0000-0000-000000000005';
INSERT INTO [dbo].[AllyProfiles] ([UserId],[OrganizationName],[AllyType],[CoverageLabel],[CoverageLat],[CoverageLng],[CoverageRadiusMetres],[VerificationStatus],[AppliedAt],[VerifiedAt])
VALUES ('AA000005-0000-0000-0000-000000000005','Refugio Central CR','Shelter',N'San Jos' + NCHAR(233) + N' Centro',9.928100,-84.090800,3000,'Verified',GETUTCDATE(),GETUTCDATE());
GO

-- ── MunicipalProfile — municipal_basica ──────────────────────────────────
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='MunicipalProfiles')
BEGIN
    DELETE FROM [dbo].[MunicipalProfiles] WHERE [UserId] IN (
        'AA000008-0000-0000-0000-000000000008','AA000009-0000-0000-0000-000000000009','AA000010-0000-0000-0000-000000000010');
    INSERT INTO [dbo].[MunicipalProfiles] ([Id],[UserId],[OrgName],[Canton],[Tier],[AdditionalCantons],[ExpiresAt],[CreatedAt])
    VALUES
        (NEWID(),'AA000008-0000-0000-0000-000000000008','Municipalidad de Desamparados','Desamparados','Basica','[]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE()),
        (NEWID(),'AA000009-0000-0000-0000-000000000009',N'Municipalidad de San Jos' + NCHAR(233),N'San Jos' + NCHAR(233),'Full','[]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE()),
        (NEWID(),'AA000010-0000-0000-0000-000000000010','Red Municipal Norte','Alajuela','RedRegional',N'["Grecia","Po' + NCHAR(225) + N's","San Carlos"]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE());
END;
GO

-- ── Verify results ────────────────────────────────────────────────────────
SELECT [Email],[Role],[Name] FROM [dbo].[Users]
WHERE [Email] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr')
ORDER BY [Role],[Email];

SELECT s.[Tier], s.[Status], u.[Email]
FROM [dbo].[Subscriptions] s
JOIN [dbo].[Users] u ON u.[Id]=s.[UserId] OR u.[Id]=s.[ClinicOwnerId]
WHERE u.[Email] IN ('owner_plus@test.cr','owner_familia@test.cr','clinica_partner@test.cr');
GO
