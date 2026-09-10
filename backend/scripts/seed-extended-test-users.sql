-- ============================================================
-- PawTrack CR — Extended Test Users & Functional Data Seed Script
-- Updated: 2026-09-10
--
-- Extends the base seed with users and functional seeded data
-- for all roles/tiers/plans in PawTrack CR:
--   1. Admin           (admin@pawtrack.cr)
--   2. Owner (Free)    (owner_free@test.cr)
--   3. Owner (Plus)    (owner_plus@test.cr)
--   4. Owner (Familia) (owner_familia@test.cr)
--   5. Ally (Shelter)  (ally@test.cr)
--   6. Clinic (Basic)  (clinica_basica@test.cr)
--   7. Clinic (Partner)(clinica_partner@test.cr)
--   8. Municipality    (municipal_basica, municipal_full, municipal_regional)
--   9. Store           (tienda_activa@test.cr)
--  10. Support         (soporte_bienestar@test.cr)
--
-- Password universal para cuentas @test.cr: Test123!
-- All accounts: IsEmailVerified = 1
--
-- Run against LOCAL dev database:
-- Server: (localdb)\MSSQLLocalDB | DB: PawTrackDev
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── 1. Limpieza idempotente de datos extendidos previos ───────────────────
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
    'AA100001-0000-0000-0000-000000000002')
   OR [Id] = 'CC104000-0000-0000-0000-000000000001';
DELETE FROM [dbo].[Pets]
WHERE [Id] IN (
    'AA100001-0000-0000-0000-000000000001',
    'AA100001-0000-0000-0000-000000000002');

DELETE FROM [dbo].[StoreOrderItems]
WHERE [OrderId] IN ('BB102000-0000-0000-0000-000000000001', 'BB102000-0000-0000-0000-000000000002');
DELETE FROM [dbo].[StoreOrders]
WHERE [Id] IN ('BB102000-0000-0000-0000-000000000001', 'BB102000-0000-0000-0000-000000000002');
DELETE FROM [dbo].[StoreProducts]
WHERE [StoreId] IN ('BB000011-0000-0000-0000-000000000011', '54A4DE57-CF32-4F07-92DF-2F8061D41C9B')
   OR [Id] IN (
    'BB101000-0000-0000-0000-000000000001', 'BB101000-0000-0000-0000-000000000002',
    'BB101000-0000-0000-0000-000000000003', 'BB101000-0000-0000-0000-000000000004');
DELETE FROM [dbo].[Stores]
WHERE [ContactEmail] = 'tienda_activa@test.cr'
   OR [Id] = 'BB000011-0000-0000-0000-000000000011';

DELETE FROM [dbo].[ClinicMedicalAccessGrants]
WHERE [ClinicId] IN ('CC100000-0000-0000-0000-000000000006', 'CC100000-0000-0000-0000-000000000007', '27EB5291-3F51-4C0F-945F-48FFD8E47BB2')
   OR [Id] = 'CC103000-0000-0000-0000-000000000007';
DELETE FROM [dbo].[ClinicVeterinarians]
WHERE [ClinicId] IN ('CC100000-0000-0000-0000-000000000006', 'CC100000-0000-0000-0000-000000000007', '27EB5291-3F51-4C0F-945F-48FFD8E47BB2')
   OR [Id] = 'CC102000-0000-0000-0000-000000000007';
DELETE FROM [dbo].[ClinicVerifications]
WHERE [ClinicId] IN ('CC100000-0000-0000-0000-000000000006', 'CC100000-0000-0000-0000-000000000007', '27EB5291-3F51-4C0F-945F-48FFD8E47BB2')
   OR [Id] = 'CC101000-0000-0000-0000-000000000007';
DELETE FROM [dbo].[Clinics]
WHERE [ContactEmail] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr')
   OR [Id] IN ('CC100000-0000-0000-0000-000000000006', 'CC100000-0000-0000-0000-000000000007');

DELETE FROM [dbo].[CapturedAnimals]
WHERE [Id] IN (
    'EE101000-0000-0000-0000-000000000001',
    'EE101000-0000-0000-0000-000000000002',
    'EE101000-0000-0000-0000-000000000003');
DELETE FROM [dbo].[MunicipalityProfiles]
WHERE [UserId] IN (
    'AA000008-0000-0000-0000-000000000008',
    'AA000009-0000-0000-0000-000000000009',
    'AA000010-0000-0000-0000-000000000010')
   OR [Id] IN (
    'EE000008-0000-0000-0000-000000000008',
    'EE000009-0000-0000-0000-000000000009',
    'EE000010-0000-0000-0000-000000000010');

DELETE FROM [dbo].[AnimalWelfareCaseNotes]
WHERE [CaseId] IN (
    'FF100000-0000-0000-0000-000000000001',
    'FF100000-0000-0000-0000-000000000002',
    'FF100000-0000-0000-0000-000000000003');
DELETE FROM [dbo].[AnimalWelfareCases]
WHERE [Id] IN (
    'FF100000-0000-0000-0000-000000000001',
    'FF100000-0000-0000-0000-000000000002',
    'FF100000-0000-0000-0000-000000000003');

DELETE FROM [dbo].[AdoptionApplications]
WHERE [Id] IN (
    'AD101000-0000-0000-0000-000000000001',
    'AD101000-0000-0000-0000-000000000002');

DELETE FROM [dbo].[AllyProfiles]
WHERE [UserId] = 'AA000005-0000-0000-0000-000000000005';

DELETE FROM [dbo].[Subscriptions]
WHERE [UserId] IN (
    'AA000001-0000-0000-0000-000000000001','AA000002-0000-0000-0000-000000000002',
    'AA000003-0000-0000-0000-000000000003','AA000004-0000-0000-0000-000000000004',
    'AA000005-0000-0000-0000-000000000005','AA000006-0000-0000-0000-000000000006',
    'AA000007-0000-0000-0000-000000000007','AA000008-0000-0000-0000-000000000008',
    'AA000009-0000-0000-0000-000000000009','AA000010-0000-0000-0000-000000000010',
    'AA000011-0000-0000-0000-000000000011','AA000012-0000-0000-0000-000000000012')
   OR [ClinicOwnerId] IN (
    'AA000001-0000-0000-0000-000000000001','AA000007-0000-0000-0000-000000000007')
   OR [PaymentReference] IN ('TESTPLUS', 'TESTFAM1', 'TESTPART');

DELETE FROM [dbo].[Users]
WHERE [Email] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr',
    'tienda_activa@test.cr','soporte_bienestar@test.cr');
GO

-- ── 2. BCrypt Hash de 'Test123!' (cost factor 12) ─────────────────────────
DECLARE @hash NVARCHAR(200) = '$2a$12$S7/AoW/NeL4KIvhZO6p6TuWsgBuZQ6kymMD96.3.ALIKR/W51wAES';

-- ── 3. Usuarios de prueba por rol ─────────────────────────────────────────
-- 1. Admin
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000001-0000-0000-0000-000000000001','admin@pawtrack.cr',@hash,'Admin PawTrack','Admin',1,0,GETUTCDATE(),1);

-- 2. Owner — Explorador (gratis, sin suscripción activa)
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000002-0000-0000-0000-000000000002','owner_free@test.cr',@hash,'Ana Libre (Free)','Owner',1,0,GETUTCDATE(),1);

-- 3. Owner — UserPlus activo
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000003-0000-0000-0000-000000000003','owner_plus@test.cr',@hash,'Pedro Plus','Owner',1,0,GETUTCDATE(),1);

-- 4. Owner — UserFamilia activo
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000004-0000-0000-0000-000000000004','owner_familia@test.cr',@hash,'Laura Familia','Owner',1,0,GETUTCDATE(),1);

-- 5. Ally — Refugio verificado (Shelter)
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000005-0000-0000-0000-000000000005','ally@test.cr',@hash,'Refugio Central CR','Ally',1,0,GETUTCDATE(),1);

-- 6. Clinic — ClinicBasic
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000006-0000-0000-0000-000000000006','clinica_basica@test.cr',@hash,'VetBasica Test','Clinic',1,0,GETUTCDATE(),1);

-- 7. Clinic — ClinicPartner
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000007-0000-0000-0000-000000000007','clinica_partner@test.cr',@hash,'VetPartner Elite','Clinic',1,0,GETUTCDATE(),1);

-- 8. Municipality — Básica (Desamparados)
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000008-0000-0000-0000-000000000008','municipal_basica@test.cr',@hash,'Muni Desamparados','Municipality',1,0,GETUTCDATE(),1);

-- 9. Municipality — Full (San José)
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000009-0000-0000-0000-000000000009','municipal_full@test.cr',@hash,N'Muni San Jos' + NCHAR(233) + N' Full','Municipality',1,0,GETUTCDATE(),1);

-- 10. Municipality — RedRegional (Norte)
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000010-0000-0000-0000-000000000010','municipal_regional@test.cr',@hash,'Red Regional Norte','Municipality',1,0,GETUTCDATE(),1);

-- 11. Store — Tienda activa
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000011-0000-0000-0000-000000000011','tienda_activa@test.cr',@hash,'PetShop CR Test','Store',1,0,GETUTCDATE(),1);

-- 12. Support — Especialista de Bienestar Animal
INSERT INTO [dbo].[Users] ([Id],[Email],[PasswordHash],[Name],[Role],[IsEmailVerified],[FailedLoginAttempts],[CreatedAt],[IsAdultConfirmed])
VALUES ('AA000012-0000-0000-0000-000000000012','soporte_bienestar@test.cr',@hash,'Soporte Bienestar Animal','Support',1,0,GETUTCDATE(),1);
GO

-- ── 4. Clínicas con IDs deterministas ──────────────────────────────────────
INSERT INTO [dbo].[Clinics] ([Id],[UserId],[Name],[LicenseNumber],[Address],[Lat],[Lng],[ContactEmail],[Status],[RegisteredAt])
VALUES
    ('CC100000-0000-0000-0000-000000000006','AA000006-0000-0000-0000-000000000006','VetBasica Test','VET-TEST-BASIC','Alajuela, Costa Rica',10.015694,-84.214631,'clinica_basica@test.cr','Active',GETUTCDATE()),
    ('CC100000-0000-0000-0000-000000000007','AA000007-0000-0000-0000-000000000007','VetPartner Elite','VET-TEST-PARTNER',N'Escaz' + NCHAR(250) + N', San Jos' + NCHAR(233),9.921538,-84.142429,'clinica_partner@test.cr','Active',GETUTCDATE());
GO

-- ── 5. Tienda con ID determinista ─────────────────────────────────────────
INSERT INTO [dbo].[Stores] ([Id],[UserId],[Name],[Description],[Address],[Lat],[Lng],[ContactEmail],[IsFeatured],[Status],[RegisteredAt])
VALUES ('BB000011-0000-0000-0000-000000000011','AA000011-0000-0000-0000-000000000011','PetShop CR Test','Tienda de prueba con catalogo y ordenes funcionales','Heredia, Costa Rica',9.998000,-84.117000,'tienda_activa@test.cr',0,1,GETUTCDATE());
GO

-- ── 6. Suscripciones activas ──────────────────────────────────────────────
INSERT INTO [dbo].[Subscriptions] ([Id],[UserId],[ClinicId],[ClinicOwnerId],[Tier],[Status],[PaymentReference],[AmountCrc],[CreatedAt],[ActivatedAt],[ExpiresAt],[CancelledAt],[PaymentReportedAt])
VALUES
    (NEWID(),'AA000003-0000-0000-0000-000000000003',NULL,NULL,'UserPlus','Active','TESTPLUS',2990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL),
    (NEWID(),'AA000004-0000-0000-0000-000000000004',NULL,NULL,'UserFamilia','Active','TESTFAM1',4990,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL),
    (NEWID(),NULL,'CC100000-0000-0000-0000-000000000007','AA000007-0000-0000-0000-000000000007','ClinicPartner','Active','TESTPART',35000,GETUTCDATE(),GETUTCDATE(),DATEADD(MONTH,1,GETUTCDATE()),NULL,NULL);
GO

-- ── 7. Cadena completa para emisión de Pasaporte de Vacunas (ClinicPartner) ─
-- Verificación de la clínica
INSERT INTO [dbo].[ClinicVerifications] ([Id],[ClinicId],[LicenseNumberSnapshot],[Status],[SubmittedAt],[VerifiedAt],[VerifiedByAdminUserId],[ExpiresAt],[DocumentUrl],[SubmittedByUserId],[SupersededAt])
VALUES ('CC101000-0000-0000-0000-000000000007','CC100000-0000-0000-0000-000000000007','VET-TEST-PARTNER','Verified',DATEADD(DAY,-30,GETUTCDATE()),DATEADD(DAY,-29,GETUTCDATE()),'AA000001-0000-0000-0000-000000000001',DATEADD(YEAR,1,CAST(GETUTCDATE() as date)),'https://blob.pawtrack.cr/docs/vet-partner-senasa.pdf','AA000007-0000-0000-0000-000000000007',NULL);

-- Médico veterinario autorizado
INSERT INTO [dbo].[ClinicVeterinarians] ([Id],[ClinicId],[FullName],[LicenseNumber],[Status],[SubmittedByUserId],[ReviewedByAdminUserId],[ReviewedAt],[ExpiresAt],[CreatedAt],[Permissions])
VALUES ('CC102000-0000-0000-0000-000000000007','CC100000-0000-0000-0000-000000000007','Dra. Valeria Rojas','CMV-CR-3482','Authorized','AA000007-0000-0000-0000-000000000007','AA000001-0000-0000-0000-000000000001',DATEADD(DAY,-29,GETUTCDATE()),DATEADD(YEAR,1,CAST(GETUTCDATE() as date)),DATEADD(DAY,-30,GETUTCDATE()),'["medical:read","medical:write","certificates:issue"]');

-- Concesión de acceso médico activo sobre la mascota Max (propiedad de owner@pawtrack.test)
INSERT INTO [dbo].[ClinicMedicalAccessGrants] ([Id],[PetId],[ClinicId],[PetOwnerId],[InitiatedBy],[CodeHash],[CodeExpiresAt],[AcceptedAt],[IsActive],[AccessExpiresAt],[Permissions],[CreatedAt])
VALUES ('CC103000-0000-0000-0000-000000000007','B1000000-0000-0000-0000-000000000001','CC100000-0000-0000-0000-000000000007','D73FC5EA-6F8F-4ADF-9756-07480962EAF3','Owner','SEEDED_ACTIVE_GRANT',DATEADD(YEAR,1,GETUTCDATE()),DATEADD(DAY,-15,GETUTCDATE()),1,DATEADD(YEAR,1,GETUTCDATE()),'["read","write","export"]',DATEADD(DAY,-15,GETUTCDATE()));

-- Registro médico con vacuna antirrábica para Max (requerida para emitir pasaporte de perros)
INSERT INTO [dbo].[MedicalRecords] (
    [Id],[PetId],[CreatedByUserId],[ClinicId],[Type],[Date],[Description],
    [VetName],[ClinicName],[NextDueDate],[DocumentUrl],[CreatedAt],[WeightKg]
) VALUES (
    'CC104000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000001','D73FC5EA-6F8F-4ADF-9756-07480962EAF3',
    'CC100000-0000-0000-0000-000000000007',0,DATEADD(MONTH,-2,CAST(GETUTCDATE() as date)),
    N'Vacuna antirr' + NCHAR(225) + N'bica anual obligatoria (Lote RAB-2026-CR)',
    'Dra. Valeria Rojas','VetPartner Elite',DATEADD(MONTH,10,CAST(GETUTCDATE() as date)),NULL,SYSDATETIMEOFFSET(),24.50
);
GO

-- ── 8. Productos y pedidos para PetShop (Store activa) ─────────────────────
INSERT INTO [dbo].[StoreProducts] ([Id],[StoreId],[Name],[Description],[Category],[PriceCrc],[ImageUrl],[IsAvailable],[CreatedAt])
VALUES
    ('BB101000-0000-0000-0000-000000000001','BB000011-0000-0000-0000-000000000011','Alimento Premium Adulto 15kg','Nutricion balanceada para razas medianas y grandes',0,38500.00,NULL,1,GETUTCDATE()),
    ('BB101000-0000-0000-0000-000000000002','BB000011-0000-0000-0000-000000000011','Collar Reflectivo Ajustable','Collar de alta visibilidad con argolla de acero inoxidable',1,8500.00,NULL,1,GETUTCDATE()),
    ('BB101000-0000-0000-0000-000000000003','BB000011-0000-0000-0000-000000000011','Snacks Dentales Caninos','Previene sarro y mejora el aliento',3,4200.00,NULL,1,GETUTCDATE()),
    ('BB101000-0000-0000-0000-000000000004','BB000011-0000-0000-0000-000000000011','Juguete Mordedor Resistente','Caucho natural duradero para enriquecimiento ambiental',4,5900.00,NULL,1,GETUTCDATE());

-- Pedido 1: Pendiente de pago (Delivery) realizado por owner@pawtrack.test
INSERT INTO [dbo].[StoreOrders] (
    [Id],[StoreId],[CustomerId],[Status],[FulfillmentType],[PaymentReference],[TotalCrc],
    [DeliveryAddress],[CustomerNote],[StoreNote],[PaymentReportedByCustomer],[PlacedAt],[ConfirmedAt],[CompletedAt],[CancelledAt],[LocationId]
) VALUES (
    'BB102000-0000-0000-0000-000000000001','BB000011-0000-0000-0000-000000000011','D73FC5EA-6F8F-4ADF-9756-07480962EAF3',
    0,0,'ORDPND01',47000.00,'San Pedro, 200m este de la UCR','Favor llamar al llegar',NULL,0,
    DATEADD(HOUR,-2,GETUTCDATE()),NULL,NULL,NULL,NULL
);

INSERT INTO [dbo].[StoreOrderItems] ([Id],[OrderId],[ProductId],[ProductName],[Quantity],[UnitPriceCrc])
VALUES
    (NEWID(),'BB102000-0000-0000-0000-000000000001','BB101000-0000-0000-0000-000000000001','Alimento Premium Adulto 15kg',1,38500.00),
    (NEWID(),'BB102000-0000-0000-0000-000000000001','BB101000-0000-0000-0000-000000000002','Collar Reflectivo Ajustable',1,8500.00);

-- Pedido 2: Confirmado con pago reportado (Pickup) realizado por owner_plus@test.cr
INSERT INTO [dbo].[StoreOrders] (
    [Id],[StoreId],[CustomerId],[Status],[FulfillmentType],[PaymentReference],[TotalCrc],
    [DeliveryAddress],[CustomerNote],[StoreNote],[PaymentReportedByCustomer],[PlacedAt],[ConfirmedAt],[CompletedAt],[CancelledAt],[LocationId]
) VALUES (
    'BB102000-0000-0000-0000-000000000002','BB000011-0000-0000-0000-000000000011','AA000003-0000-0000-0000-000000000003',
    2,1,'ORDCNF02',10100.00,NULL,'Paso a recoger en la tarde','Pedido preparado en mostrador',1,
    DATEADD(DAY,-1,GETUTCDATE()),DATEADD(HOUR,-20,GETUTCDATE()),NULL,NULL,NULL
);

INSERT INTO [dbo].[StoreOrderItems] ([Id],[OrderId],[ProductId],[ProductName],[Quantity],[UnitPriceCrc])
VALUES
    (NEWID(),'BB102000-0000-0000-0000-000000000002','BB101000-0000-0000-0000-000000000003','Snacks Dentales Caninos',1,4200.00),
    (NEWID(),'BB102000-0000-0000-0000-000000000002','BB101000-0000-0000-0000-000000000004','Juguete Mordedor Resistente',1,5900.00);
GO

-- ── 9. Perfiles y capturas municipales (Municipality) ─────────────────────
INSERT INTO [dbo].[MunicipalityProfiles] ([Id],[UserId],[Canton],[OrgName],[Tier],[AdditionalCantons],[IsActive],[SubscribedAt],[ExpiresAt])
VALUES
    ('EE000008-0000-0000-0000-000000000008','AA000008-0000-0000-0000-000000000008','Desamparados','Municipalidad de Desamparados','Basica',NULL,1,GETUTCDATE(),DATEADD(YEAR,1,GETUTCDATE())),
    ('EE000009-0000-0000-0000-000000000009','AA000009-0000-0000-0000-000000000009',N'San Jos' + NCHAR(233),N'Municipalidad de San Jos' + NCHAR(233),'Full',NULL,1,GETUTCDATE(),DATEADD(YEAR,1,GETUTCDATE())),
    ('EE000010-0000-0000-0000-000000000010','AA000010-0000-0000-0000-000000000010','Alajuela','Red Regional Norte','RedRegional',N'Grecia,Po' + NCHAR(225) + N's,San Carlos',1,GETUTCDATE(),DATEADD(YEAR,1,GETUTCDATE()));

INSERT INTO [dbo].[CapturedAnimals] (
    [Id],[Canton],[Species],[Breed],[Color],[EstimatedAge],[PhotoUrl],[Notes],[CollarChipNumber],[MatchedPetId],[Status],[CapturedAt],[CreatedAt],[RecordedByUserId]
) VALUES
    ('EE101000-0000-0000-0000-000000000001','Desamparados','Perro','Golden Retriever mestizo','Dorado','Adulto (~4 a)',NULL,'Encontrado deambulando en parque de Desamparados. Vinculado por microchip declarado.','985141000100001','AA100001-0000-0000-0000-000000000001','Received',DATEADD(HOUR,-6,GETUTCDATE()),DATEADD(HOUR,-6,GETUTCDATE()),'AA000008-0000-0000-0000-000000000008'),
    ('EE101000-0000-0000-0000-000000000002',N'San Jos' + NCHAR(233),'Gato','Criollo','Blanco con gris','Joven (~1 a)',NULL,'Rescatado en inmediaciones de La Sabana. Sin microchip identificado.',NULL,NULL,'Received',DATEADD(DAY,-1,GETUTCDATE()),DATEADD(DAY,-1,GETUTCDATE()),'AA000009-0000-0000-0000-000000000009'),
    ('EE101000-0000-0000-0000-000000000003','Alajuela','Perro','Mestizo','Negro','Adulto (~5 a)',NULL,'Trasladado temporalmente a custodia municipal. Contacto con dueno en curso.','985141000200001','CC000001-0000-0000-0000-000000000001','OwnerFound',DATEADD(DAY,-2,GETUTCDATE()),DATEADD(DAY,-2,GETUTCDATE()),'AA000010-0000-0000-0000-000000000010');
GO

-- ── 10. Casos de bienestar animal para triage (Support / Bienestar) ────────
INSERT INTO [dbo].[AnimalWelfareCases] (
    [Id],[PublicCode],[Type],[Status],[Severity],[Canton],[ApproxLat],[ApproxLng],
    [DescriptionSanitized],[ReporterUserId],[ReporterIsAnonymous],[AssignedOrganizationUserId],
    [AssignedRole],[ClosureReason],[CreatedAt],[UpdatedAt],[ClosedAt]
) VALUES
    ('FF100000-0000-0000-0000-000000000001','WC-2026-A1B2','Abandonment','Received','Medium',N'San Jos' + NCHAR(233),9.9320,-84.0810,'Perro dejado en lote baldio en Barrio Escalante con poca agua y sin alimento aparente.','AA000002-0000-0000-0000-000000000002',0,NULL,NULL,NULL,DATEADD(HOUR,-4,GETUTCDATE()),DATEADD(HOUR,-4,GETUTCDATE()),NULL),
    ('FF100000-0000-0000-0000-000000000002','WC-2026-C3D4','SuspectedAbuse','Triage','High','Heredia',9.9985,-84.1165,'Mascota canina encadenada bajo la lluvia continua sin techo ni resguardo en patio frontal.','AA000003-0000-0000-0000-000000000003',0,NULL,NULL,NULL,DATEADD(DAY,-1,GETUTCDATE()),DATEADD(HOUR,-12,GETUTCDATE()),NULL),
    ('FF100000-0000-0000-0000-000000000003','WC-2026-E5F6','InjuredAnimal','Assigned','Critical','Desamparados',9.8970,-84.0680,'Perro atropellado en carretera principal con lesion aparente en extremidad posterior.',NULL,1,'AA000005-0000-0000-0000-000000000005','Shelter',NULL,DATEADD(DAY,-2,GETUTCDATE()),DATEADD(HOUR,-6,GETUTCDATE()),NULL);

INSERT INTO [dbo].[AnimalWelfareCaseNotes] ([Id],[CaseId],[AuthorUserId],[Body],[CreatedAt])
VALUES
    (NEWID(),'FF100000-0000-0000-0000-000000000002','AA000012-0000-0000-0000-000000000012','Caso clasificado con severidad Alta por exposicion a condiciones climaticas severas. Se coordinara inspeccion.',DATEADD(HOUR,-12,GETUTCDATE())),
    (NEWID(),'FF100000-0000-0000-0000-000000000003','AA000012-0000-0000-0000-000000000012','Asignado de emergencia a Refugio Central CR para traslado y evaluacion veterinaria.',DATEADD(HOUR,-6,GETUTCDATE()));
GO

-- ── 11. Perfil de Aliado (Shelter) y Solicitudes de Adopción ──────────────
INSERT INTO [dbo].[AllyProfiles] ([UserId],[OrganizationName],[AllyType],[CoverageLabel],[CoverageLat],[CoverageLng],[CoverageRadiusMetres],[VerificationStatus],[AppliedAt],[VerifiedAt])
VALUES ('AA000005-0000-0000-0000-000000000005','Refugio Central CR','Shelter',N'San Jos' + NCHAR(233) + N' Centro',9.928100,-84.090800,3000,'Verified',GETUTCDATE(),GETUTCDATE());

-- Solicitudes de adopción para probar /mis-adopciones y /shelter/animales/:id/aplicaciones
INSERT INTO [dbo].[AdoptionApplications] ([Id],[AdoptablePetId],[ApplicantUserId],[ApplicantNote],[Status],[ReviewNote],[AppliedAt],[ReviewedAt])
VALUES
    ('AD101000-0000-0000-0000-000000000001','AD200001-0000-0000-0000-000000000001','AA000002-0000-0000-0000-000000000002','Hola, me interesa mucho adoptar a Luna. Tengo casa con patio cercado y experiencia previa con mascotas.','Pending',NULL,DATEADD(DAY,-1,GETUTCDATE()),NULL),
    ('AD101000-0000-0000-0000-000000000002','AD200002-0000-0000-0000-000000000002','AA000003-0000-0000-0000-000000000003','Contamos con espacio amplio y rutina diaria de ejercicio para un perro activo como Max.','UnderReview','En verificacion de condiciones de hogar.',DATEADD(DAY,-3,GETUTCDATE()),DATEADD(DAY,-1,GETUTCDATE()));
GO

-- ── 12. Mascotas y expediente clínico — owner_familia ──────────────────────
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
GO

-- ── 13. Resumen de verificación ───────────────────────────────────────────
SELECT 'Usuarios Seed' AS Tipo, COUNT(*) AS Total FROM [dbo].[Users]
WHERE [Email] IN (
    'admin@pawtrack.cr','owner_free@test.cr','owner_plus@test.cr','owner_familia@test.cr',
    'ally@test.cr','clinica_basica@test.cr','clinica_partner@test.cr',
    'municipal_basica@test.cr','municipal_full@test.cr','municipal_regional@test.cr',
    'tienda_activa@test.cr','soporte_bienestar@test.cr')
UNION ALL SELECT 'Clinicas', COUNT(*) FROM [dbo].[Clinics] WHERE [Id] IN ('CC100000-0000-0000-0000-000000000006','CC100000-0000-0000-0000-000000000007')
UNION ALL SELECT 'Tiendas', COUNT(*) FROM [dbo].[Stores] WHERE [Id]='BB000011-0000-0000-0000-000000000011'
UNION ALL SELECT 'Productos Tienda', COUNT(*) FROM [dbo].[StoreProducts] WHERE [StoreId]='BB000011-0000-0000-0000-000000000011'
UNION ALL SELECT 'Ordenes Tienda', COUNT(*) FROM [dbo].[StoreOrders] WHERE [StoreId]='BB000011-0000-0000-0000-000000000011'
UNION ALL SELECT 'Perfiles Municipales', COUNT(*) FROM [dbo].[MunicipalityProfiles] WHERE [UserId] IN ('AA000008-0000-0000-0000-000000000008','AA000009-0000-0000-0000-000000000009','AA000010-0000-0000-0000-000000000010')
UNION ALL SELECT 'Capturas Municipales', COUNT(*) FROM [dbo].[CapturedAnimals] WHERE [Id] IN ('EE101000-0000-0000-0000-000000000001','EE101000-0000-0000-0000-000000000002','EE101000-0000-0000-0000-000000000003')
UNION ALL SELECT 'Casos Bienestar', COUNT(*) FROM [dbo].[AnimalWelfareCases] WHERE [Id] IN ('FF100000-0000-0000-0000-000000000001','FF100000-0000-0000-0000-000000000002','FF100000-0000-0000-0000-000000000003')
UNION ALL SELECT 'Solicitudes Adopcion', COUNT(*) FROM [dbo].[AdoptionApplications] WHERE [Id] IN ('AD101000-0000-0000-0000-000000000001','AD101000-0000-0000-0000-000000000002');
GO
