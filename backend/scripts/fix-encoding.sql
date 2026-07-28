-- fix-encoding.sql  — UTF-16 literals para caracteres especiales
UPDATE AllyProfiles 
SET CoverageLabel = N'Cartago y Gran ' + NCHAR(193) + N'rea Metropolitana'
WHERE UserId = 'A0000001-0000-0000-0000-000000000002';

UPDATE Clinics 
SET Name    = N'Veterinaria Los ' + NCHAR(193) + N'ngeles',
    Address = N'Barrio Los ' + NCHAR(193) + N'ngeles, Alajuela, frente al parque'
WHERE Id = 'C0000002-0000-0000-0000-000000000002';

SELECT OrganizationName, CoverageLabel FROM AllyProfiles WHERE UserId = 'A0000001-0000-0000-0000-000000000002';
SELECT Name, Address FROM Clinics WHERE Id = 'C0000002-0000-0000-0000-000000000002';
