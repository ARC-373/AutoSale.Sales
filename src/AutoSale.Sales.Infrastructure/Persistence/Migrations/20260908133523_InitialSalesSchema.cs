using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSale.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSalesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_callbacks",
                columns: table => new
                {
                    payment_code = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_callbacks", x => x.payment_code);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    buyer_cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    expected_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payment_code = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    snapshot_make = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    snapshot_model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    snapshot_year = table.Column<int>(type: "integer", nullable: true),
                    snapshot_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    snapshot_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    snapshot_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    snapshot_version = table.Column<int>(type: "integer", nullable: true),
                    snapshot_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sale_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payment_registered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payment_occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lease_owner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_catalog",
                columns: table => new
                {
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    source_version = table.Column<int>(type: "integer", nullable: false),
                    source_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    synchronized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_catalog", x => x.vehicle_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_payment_callbacks_event_id",
                table: "payment_callbacks",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_completed_price_id",
                table: "sales",
                columns: new[] { "sale_price", "id" },
                filter: "\"state\" = 'Completed'");

            migrationBuilder.CreateIndex(
                name: "ix_sales_pending_claim",
                table: "sales",
                columns: new[] { "state", "next_attempt_at_utc", "lease_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_sales_active_vehicle",
                table: "sales",
                column: "vehicle_id",
                unique: true,
                filter: "\"state\" NOT IN ('Cancelled', 'Rejected')");

            migrationBuilder.CreateIndex(
                name: "ux_sales_buyer_idempotency",
                table: "sales",
                columns: new[] { "buyer_subject", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_code",
                table: "sales",
                column: "payment_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_catalog_status_price_id",
                table: "vehicle_catalog",
                columns: new[] { "status", "price", "vehicle_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_callbacks");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "vehicle_catalog");
        }
    }
}
