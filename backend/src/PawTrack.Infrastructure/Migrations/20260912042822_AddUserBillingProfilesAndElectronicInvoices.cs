using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBillingProfilesAndElectronicInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectronicInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaveNumerica = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroConsecutivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoCabys = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SubtotalCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IvaRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    IvaAmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalAmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaymentMethodCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ReceiverIdType = table.Column<int>(type: "int", nullable: true),
                    ReceiverIdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReceiverEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignedXmlUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HaciendaResponseXmlUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PdfRepresentationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HaciendaResponseStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HaciendaErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedByHaciendaAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificationType = table.Column<int>(type: "int", nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BillingEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Canton = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    District = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressDetails = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RequiresInvoice = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBillingProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_ClaveNumerica",
                table: "ElectronicInvoices",
                column: "ClaveNumerica",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_NumeroConsecutivo",
                table: "ElectronicInvoices",
                column: "NumeroConsecutivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_Status",
                table: "ElectronicInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicInvoices_UserId",
                table: "ElectronicInvoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBillingProfiles_IdentificationNumber",
                table: "UserBillingProfiles",
                column: "IdentificationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_UserBillingProfiles_UserId",
                table: "UserBillingProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicInvoices");

            migrationBuilder.DropTable(
                name: "UserBillingProfiles");
        }
    }
}
