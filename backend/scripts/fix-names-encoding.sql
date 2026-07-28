-- fix-names-encoding.sql
-- Corrects UTF-8 double-encoded names in Users and Clinics tables.
-- Each accented char stored as 2 bytes (UTF-8) instead of 1 Unicode code point.

-- Ana Pérez (é = NCHAR(233))
UPDATE Users SET Name = N'Ana P' + NCHAR(233) + N'rez (Owner)'
WHERE Id = 'D73FC5EA-6F8F-4ADF-9756-07480962EAF3';

-- Clínica VetCare CR (í = NCHAR(237))
UPDATE Users SET Name = N'Cl' + NCHAR(237) + N'nica VetCare CR'
WHERE Id = '2B9B9F17-39DD-42A7-B138-A00632ABE55A';

-- Clínica Animal House
UPDATE Users SET Name = N'Cl' + NCHAR(237) + N'nica Animal House'
WHERE Id = 'C0000001-0000-0000-0000-000000000001';

-- Veterinaria Los Ángeles (Á = NCHAR(193))
UPDATE Users SET Name = N'Veterinaria Los ' + NCHAR(193) + N'ngeles'
WHERE Id = 'C0000001-0000-0000-0000-000000000002';

-- Fundación Patitas Felices (ó = NCHAR(243))
UPDATE Users SET Name = N'Fundaci' + NCHAR(243) + N'n Patitas Felices'
WHERE Id = 'A0000001-0000-0000-0000-000000000002';

-- Also fix Clinics table names
UPDATE Clinics SET Name = N'Cl' + NCHAR(237) + N'nica VetCare CR'
WHERE UserId = '2B9B9F17-39DD-42A7-B138-A00632ABE55A';

UPDATE Clinics SET Name = N'Cl' + NCHAR(237) + N'nica Animal House'
WHERE UserId = 'C0000001-0000-0000-0000-000000000001';

-- Verify
SELECT Id, Name FROM Users ORDER BY Name;
SELECT Name FROM Clinics;
