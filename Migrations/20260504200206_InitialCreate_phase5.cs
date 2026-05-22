using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfriPay.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_phase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "payments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "merchants",
                newName: "status");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "merchants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                table: "merchants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refresh_token_expires_at",
                table: "merchants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Disputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ChargebackFee = table.Column<long>(type: "bigint", nullable: false),
                    ProviderReference = table.Column<string>(type: "text", nullable: true),
                    ProviderKey = table.Column<string>(type: "text", nullable: true),
                    CustomerNote = table.Column<string>(type: "text", nullable: true),
                    MerchantNote = table.Column<string>(type: "text", nullable: true),
                    ResolutionNote = table.Column<string>(type: "text", nullable: true),
                    RespondBy = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disputes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    refresh_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    refresh_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisputeEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_status",
                table: "payments",
                column: "status",
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "idx_merchants_refresh_token",
                table: "merchants",
                column: "refresh_token",
                filter: "refresh_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_DisputeId",
                table: "DisputeEvidence",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "idx_employees_email",
                table: "employees",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_employees_merchant_email",
                table: "employees",
                columns: new[] { "merchant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_employees_refresh_token",
                table: "employees",
                column: "refresh_token",
                filter: "refresh_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_staff_email",
                table: "staff_members",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_staff_refresh_token",
                table: "staff_members",
                column: "refresh_token",
                filter: "refresh_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisputeEvidence");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "staff_members");

            migrationBuilder.DropTable(
                name: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_payments_status",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "idx_merchants_refresh_token",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                table: "merchants");

            migrationBuilder.DropColumn(
                name: "refresh_token_expires_at",
                table: "merchants");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "payments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "merchants",
                newName: "Status");
        }
    }
}
