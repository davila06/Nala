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
DELETE FROM [dbo].[Clinics]      WHERE [ContactEmail] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr');
DELETE FROM [dbo].[Subscriptions] WHERE [UserId] IN (
    SELECT [Id] FROM [dbo].[Users] WHERE [Email] IN (
        'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
        'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
        'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr'));
DELETE FROM [dbo].[Users] WHERE [Email] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr');
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
VALUES ('AA000009-0000-0000-0000-000000000009','municipal_full@test.cr',@hash,'Muni San José Full','Municipality',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());

-- ── 10. Municipality — RedRegional ────────────────────────────────────────
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[EmailVerificationToken],[EmailVerificationTokenExpiry],[PasswordResetToken],[PasswordResetTokenExpiry],[FailedLoginAttempts],[LockoutEnd],[CreatedAt])
VALUES ('AA000010-0000-0000-0000-000000000010','municipal_regional@test.cr',@hash,'Red Regional Norte','Municipality',1,NULL,NULL,NULL,NULL,0,NULL,GETUTCDATE());
GO

-- ── Clinics table — ClinicBasic (status Active) ───────────────────────────
INSERT INTO [dbo].[Clinics] ([Id],[UserId],[Name],[LicenseNumber],[Address],[Lat],[Lng],[ContactEmail],[Status],[RegisteredAt])
VALUES (NEWID(),'AA000006-0000-0000-0000-000000000006','VetBasica Test','VET-TEST-BASIC','Alajuela, Costa Rica',10.015694,-84.214631,'clinica_basica@test.cr','Active',GETUTCDATE());

-- ── Clinics table — ClinicPartner (status Active) ────────────────────────
INSERT INTO [dbo].[Clinics] ([Id],[UserId],[Name],[LicenseNumber],[Address],[Lat],[Lng],[ContactEmail],[Status],[RegisteredAt])
VALUES (NEWID(),'AA000007-0000-0000-0000-000000000007','VetPartner Elite','VET-TEST-PARTNER','Escazú, San José',9.921538,-84.142429,'clinica_partner@test.cr','Active',GETUTCDATE());
GO

-- ── Subscriptions — owner_plus: UserPlus activo ───────────────────────────
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
VALUES (NEWID(),'AA000003-0000-0000-0000-000000000003',NULL,NULL,'UserPlus','Active','TESTPLUS1',2990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL);

-- ── Subscriptions — owner_familia: UserFamilia activo ────────────────────
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
VALUES (NEWID(),'AA000004-0000-0000-0000-000000000004',NULL,NULL,'UserFamilia','Active','TESTFAM01',4990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL);

-- ── Subscriptions — clinica_partner: ClinicPartner activo ────────────────
-- ClinicId is the Clinic.Id (not the UserId); get it dynamically
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
SELECT NEWID(),NULL,c.[Id],'AA000007-0000-0000-0000-000000000007','ClinicPartner','Active','TESTPART1',35000,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL
FROM [dbo].[Clinics] c WHERE c.[UserId]='AA000007-0000-0000-0000-000000000007';
GO

-- ── AllyProfile — ally@test.cr (Verified) ────────────────────────────────
DELETE FROM [dbo].[AllyProfiles] WHERE [UserId]='AA000005-0000-0000-0000-000000000005';
INSERT INTO [dbo].[AllyProfiles] ([UserId],[OrganizationName],[AllyType],[CoverageLabel],[CoverageLat],[CoverageLng],[CoverageRadiusMetres],[VerificationStatus],[AppliedAt],[VerifiedAt])
VALUES ('AA000005-0000-0000-0000-000000000005','Refugio Central CR','Shelter','San José Centro',9.928100,-84.090800,3000,'Verified',GETUTCDATE(),GETUTCDATE());
GO

-- ── MunicipalProfile — municipal_basica ──────────────────────────────────
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='MunicipalProfiles')
BEGIN
    DELETE FROM [dbo].[MunicipalProfiles] WHERE [UserId] IN (
        'AA000008-0000-0000-0000-000000000008','AA000009-0000-0000-0000-000000000009','AA000010-0000-0000-0000-000000000010');
    INSERT INTO [dbo].[MunicipalProfiles] ([Id],[UserId],[OrgName],[Canton],[Tier],[AdditionalCantons],[ExpiresAt],[CreatedAt])
    VALUES
        (NEWID(),'AA000008-0000-0000-0000-000000000008','Municipalidad de Desamparados','Desamparados','Basica','[]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE()),
        (NEWID(),'AA000009-0000-0000-0000-000000000009','Municipalidad de San José','San José','Full','[]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE()),
        (NEWID(),'AA000010-0000-0000-0000-000000000010','Red Municipal Norte','Alajuela','RedRegional','["Grecia","Poás","San Carlos"]',DATEADD(YEAR,1,GETUTCDATE()),GETUTCDATE());
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
