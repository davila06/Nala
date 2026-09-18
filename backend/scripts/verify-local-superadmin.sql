-- PawTrack CR - Local SuperAdmin verification (no secrets)
-- Usage:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -i backend/scripts/verify-local-superadmin.sql

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Email nvarchar(254) = N'superadmin@pawtrack.local';

SELECT
    CONVERT(varchar(36), u.Id) AS UserId,
    u.Email,
    u.Role,
    u.IsEmailVerified,
    u.MfaEnabled,
    u.MfaConfiguredAt,
    u.IsDeleted
FROM dbo.Users AS u
WHERE u.Email = @Email;

SELECT COUNT(*) AS SuperAdminAuditEvents
FROM dbo.AuditLog AS a
INNER JOIN dbo.Users AS u ON a.EntityId = CONVERT(nvarchar(36), u.Id)
WHERE u.Email = @Email
  AND a.Action IN (N'SuperAdminAssigned', N'SuperAdminRevoked');

SELECT TOP (3) MigrationId
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId DESC;
