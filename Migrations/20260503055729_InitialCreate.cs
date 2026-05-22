using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfriPay.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    to_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    OfficialRate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Spread = table.Column<decimal>(type: "numeric(6,5)", nullable: false),
                    EffectiveRate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                },
                comment: "Append-only. Never UPDATE. Insert new row for each rate refresh.");

            migrationBuilder.CreateTable(
                name: "merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pricing_plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    webhook_url = table.Column<string>(type: "text", nullable: true),
                    webhook_secret = table.Column<string>(type: "text", nullable: true),
                    monthly_tx_limit = table.Column<int>(type: "integer", nullable: false),
                    rate_per_minute = table.Column<int>(type: "integer", nullable: false),
                    all_providers = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    fee_amount = table.Column<long>(type: "bigint", nullable: true),
                    fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    net_amount = table.Column<long>(type: "bigint", nullable: true),
                    net_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProviderUsed = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ProviderReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    customer_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    customer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    WebhookUrl = table.Column<string>(type: "text", nullable: true),
                    ussd_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IsPartial = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ProviderReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EstimatedArrival = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Signature = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetUrl = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_deliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    key_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_keys_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalTable: "merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merchant_kyb",
                columns: table => new
                {
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    SubmittedAt = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_kyb", x => x.merchant_id);
                    table.ForeignKey(
                        name: "FK_merchant_kyb_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalTable: "merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_status_transitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_status_transitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_status_transitions_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderReference = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_attempts_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_delivery_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_webhook_delivery_attempts_webhook_deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "webhook_deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_api_keys_hash_active",
                table: "api_keys",
                column: "key_hash",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "idx_api_keys_merchant_id",
                table: "api_keys",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_from_currency_to_currency",
                table: "exchange_rates",
                columns: new[] { "from_currency", "to_currency" });

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_RecordedAt",
                table: "exchange_rates",
                column: "RecordedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_merchants_Email",
                table: "merchants",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_transitions_PaymentId",
                table: "payment_status_transitions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_MerchantId_IdempotencyKey",
                table: "payments",
                columns: new[] { "MerchantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_attempts_PaymentId",
                table: "provider_attempts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_MerchantId_CreatedAt",
                table: "refunds",
                columns: new[] { "MerchantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_PaymentId",
                table: "refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_status",
                table: "refunds",
                column: "status",
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_EventType",
                table: "webhook_deliveries",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_MerchantId",
                table: "webhook_deliveries",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_NextRetryAt",
                table: "webhook_deliveries",
                column: "NextRetryAt",
                filter: "status IN ('pending','retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_DeliveryId",
                table: "webhook_delivery_attempts",
                column: "DeliveryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "merchant_kyb");

            migrationBuilder.DropTable(
                name: "payment_status_transitions");

            migrationBuilder.DropTable(
                name: "provider_attempts");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "webhook_delivery_attempts");

            migrationBuilder.DropTable(
                name: "merchants");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "webhook_deliveries");
        }
    }
}
