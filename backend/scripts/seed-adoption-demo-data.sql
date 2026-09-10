-- PawTrack CR - adoption demo data
-- Local development only: (localdb)\MSSQLLocalDB / PawTrackDev
-- Prerequisite: migrations applied and ally@test.cr verified as Shelter.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @organizationUserId uniqueidentifier = 'AA000005-0000-0000-0000-000000000005';
DECLARE @fairId uniqueidentifier = 'AD300001-0000-0000-0000-000000000001';
DECLARE @lunaId uniqueidentifier = 'AD200001-0000-0000-0000-000000000001';
DECLARE @maxId uniqueidentifier = 'AD200002-0000-0000-0000-000000000002';
DECLARE @cocoId uniqueidentifier = 'AD200003-0000-0000-0000-000000000003';
DECLARE @nalaId uniqueidentifier = 'AD200004-0000-0000-0000-000000000004';

DELETE FROM dbo.AdoptionFairs WHERE Id = @fairId;
DELETE FROM dbo.AdoptionApplications WHERE AdoptablePetId IN (@lunaId, @maxId, @cocoId, @nalaId);
DELETE FROM dbo.AdoptableAnimals WHERE Id IN (@lunaId, @maxId, @cocoId, @nalaId);

INSERT INTO dbo.AdoptableAnimals (
    Id, OrganizationUserId, Name, Species, Breed, Size, AgeCategory, AgeMonthsApprox,
    Story, Requirements, MedicalNotes, IsVaccinated, IsSterilized, IsMicrochipped,
    OkWithKids, OkWithDogs, OkWithCats, NeedsYard, RefLat, RefLng, RefLabel, Status,
    PublishedAt, UpdatedAt, AdoptedAt, PhotoUrls)
VALUES
    (@lunaId, @organizationUserId, 'Luna', 'Dog', 'Mestiza mediana', 'Medium', 'Young', 20,
     'Luna es una perrita sociable y juguetona que disfruta los paseos tranquilos y aprender trucos.',
     'Familia comprometida con paseos diarios y adaptación gradual.',
     'Control veterinario al día; requiere continuar suplemento articular durante 30 días.',
     1, 1, 0, 1, 1, 1, 0, 9.998200, -84.116700, 'Heredia centro', 'Available',
     SYSDATETIMEOFFSET(), NULL, NULL, '[]'),
    (@maxId, @organizationUserId, 'Max', 'Dog', 'Labrador mestizo', 'Large', 'Adult', 60,
     'Max es un perro noble y estable, ideal para una familia activa con espacio para compartir.',
     'Preferiblemente hogar con patio y experiencia previa con perros grandes.',
     'Vacunas completas; esterilizado y en buen estado general.',
     1, 1, 0, 1, 1, 0, 1, 10.002100, -84.115200, 'San Rafael de Heredia', 'Available',
     SYSDATETIMEOFFSET(), NULL, NULL, '[]'),
    (@cocoId, @organizationUserId, 'Coco', 'Cat', 'Criollo', 'Small', 'Young', 10,
     'Coco es un gato curioso y cariñoso que busca un hogar tranquilo para convertirse en compañía permanente.',
     'Vivienda segura con ventanas protegidas y compromiso de adaptación en interiores.',
     'Vacuna inicial aplicada; pendiente refuerzo según calendario veterinario.',
     0, 0, 0, 1, 0, 1, 0, 9.996700, -84.120100, 'Barva', 'Available',
     SYSDATETIMEOFFSET(), NULL, NULL, '[]'),
    (@nalaId, @organizationUserId, 'Nala', 'Dog', 'Pastor mestiza', 'Large', 'Puppy', 8,
     'Nala es una cachorra activa y afectuosa que necesita una familia paciente para acompañar su crecimiento.',
     'Se solicita compromiso de entrenamiento, socialización y esterilización futura.',
     'Desparasitación al día; próxima vacuna programada.',
     1, 0, 0, 1, 1, 1, 1, 10.005400, -84.110800, 'Santo Domingo', 'Available',
     SYSDATETIMEOFFSET(), NULL, NULL, '[]');

INSERT INTO dbo.AdoptionFairs (
    Id, OrganizationUserId, Title, Description, VenueLabel, Lat, Lng, StartsAt, EndsAt,
    Status, CreatedAt, UpdatedAt, AnimalIds)
VALUES (
    @fairId, @organizationUserId, 'Feria de adopción Refugio Central',
    'Conoce a nuestros animales disponibles y recibe orientación para una adopción responsable.',
    'Parque Central de Heredia', 9.998200, -84.116700,
    DATEADD(DAY, 7, SYSDATETIMEOFFSET()), DATEADD(DAY, 7, DATEADD(HOUR, 4, SYSDATETIMEOFFSET())),
    'Upcoming', SYSDATETIMEOFFSET(), NULL,
    CONCAT('["', CONVERT(varchar(36), @lunaId), '","', CONVERT(varchar(36), @maxId), '","',
        CONVERT(varchar(36), @cocoId), '","', CONVERT(varchar(36), @nalaId), '"]'));

COMMIT;

SELECT 'AdoptableAnimals' AS EntityName, COUNT(*) AS Seeded
FROM dbo.AdoptableAnimals
WHERE Id IN (@lunaId, @maxId, @cocoId, @nalaId)
UNION ALL
SELECT 'AdoptionFairs', COUNT(*)
FROM dbo.AdoptionFairs
WHERE Id = @fairId;
