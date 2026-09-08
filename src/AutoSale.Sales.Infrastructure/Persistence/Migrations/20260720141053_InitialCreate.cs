using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoSale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    make = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_price_positive", "price > 0");
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    purchased_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.id);
                    table.CheckConstraint("ck_sales_price_positive", "sale_price > 0");
                    table.ForeignKey(
                        name: "FK_sales_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_price_id",
                table: "sales",
                columns: new[] { "sale_price", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_sales_buyer_idempotency",
                table: "sales",
                columns: new[] { "buyer_subject", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_sales_vehicle_id",
                table: "sales",
                column: "vehicle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_status_price_id",
                table: "vehicles",
                columns: new[] { "status", "price", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
