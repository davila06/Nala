using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingUserLockoutColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Users', 'FailedLoginAttempts') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Users]
                    ADD [FailedLoginAttempts] int NOT NULL CONSTRAINT [DF_Users_FailedLoginAttempts] DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Users', 'LockoutEnd') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Users]
                    ADD [LockoutEnd] datetimeoffset NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Users', 'LockoutEnd') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Users]
                    DROP COLUMN [LockoutEnd];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Users', 'FailedLoginAttempts') IS NOT NULL
                BEGIN
                    DECLARE @ConstraintName nvarchar(128);

                    SELECT @ConstraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    INNER JOIN sys.tables t
                        ON t.object_id = c.object_id
                    INNER JOIN sys.schemas s
                        ON s.schema_id = t.schema_id
                    WHERE s.name = 'dbo'
                        AND t.name = 'Users'
                        AND c.name = 'FailedLoginAttempts';

                    IF @ConstraintName IS NOT NULL
                    BEGIN
                        EXEC('ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @ConstraintName + ']');
                    END

                    ALTER TABLE [dbo].[Users]
                    DROP COLUMN [FailedLoginAttempts];
                END
                """);
        }
    }
}
