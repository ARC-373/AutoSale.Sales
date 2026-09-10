using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSale.Payments.Mock.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPaymentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_code = table.Column<Guid>(type: "TEXT", nullable: false),
                    sale_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    occurred_at_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    callback_delivered_at_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    next_attempt_at_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    lease_owner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    lease_expires_at_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.payment_code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_callback_delivered_at_utc_next_attempt_at_utc",
                table: "payments",
                columns: new[] { "callback_delivered_at_utc", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_event_id",
                table: "payments",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_sale_id",
                table: "payments",
                column: "sale_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
