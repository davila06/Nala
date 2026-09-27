SET NOCOUNT ON;

DECLARE @customer uniqueidentifier = CONVERT(uniqueidentifier, HASHBYTES('MD5', 'customer-7'));
DECLARE @pet uniqueidentifier = CONVERT(uniqueidentifier, HASHBYTES('MD5', 'pet-3'));
DECLARE @rangeStart datetimeoffset = '2026-06-01T00:00:00+00:00';
DECLARE @rangeEnd datetimeoffset = '2026-07-01T00:00:00+00:00';

CREATE TABLE #ProviderBookings
(
    Id uniqueidentifier NOT NULL,
    CustomerUserId uniqueidentifier NOT NULL,
    PetId uniqueidentifier NOT NULL,
    StartsAt datetimeoffset NOT NULL,
    EndsAt datetimeoffset NOT NULL,
    CreatedAt datetimeoffset NOT NULL
);

WITH numbers AS
(
    SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
    FROM sys.all_objects AS first_set CROSS JOIN sys.all_objects AS second_set
)
INSERT INTO #ProviderBookings (Id, CustomerUserId, PetId, StartsAt, EndsAt, CreatedAt)
SELECT NEWID(),
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('customer-', n % 20))),
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('pet-', n / 20 % 20))),
       DATEADD(day, n % 365, CONVERT(datetimeoffset, '2026-01-01T00:00:00+00:00')),
       DATEADD(hour, 1, DATEADD(day, n % 365, CONVERT(datetimeoffset, '2026-01-01T00:00:00+00:00'))),
       DATEADD(day, n % 365, CONVERT(datetimeoffset, '2026-01-01T00:00:00+00:00'))
FROM numbers;

CREATE INDEX IX_Baseline ON #ProviderBookings (CustomerUserId, CreatedAt);
SELECT 'BASELINE: 100000 synthetic bookings' AS Phase, COUNT(*) AS RowsInSample FROM #ProviderBookings;

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
SELECT TOP (100) Id, PetId, StartsAt, EndsAt
FROM #ProviderBookings WITH (INDEX(IX_Baseline))
WHERE CustomerUserId = @customer AND PetId = @pet
  AND StartsAt < @rangeEnd AND EndsAt > @rangeStart
ORDER BY StartsAt, Id;

CREATE INDEX IX_Calendar ON #ProviderBookings (CustomerUserId, PetId, StartsAt);
SELECT 'PROPOSED INDEX: same query and sample' AS Phase;
SELECT TOP (100) Id, PetId, StartsAt, EndsAt
FROM #ProviderBookings WITH (INDEX(IX_Calendar))
WHERE CustomerUserId = @customer AND PetId = @pet
  AND StartsAt < @rangeEnd AND EndsAt > @rangeStart
ORDER BY StartsAt, Id;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- Session-scoped #tables disappear automatically; no application data is changed.