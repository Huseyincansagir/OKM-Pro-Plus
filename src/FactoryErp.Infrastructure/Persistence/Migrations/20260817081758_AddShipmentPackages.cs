using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipment_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    package_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    package_count = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    quantity_base_per_package = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    package_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    physical_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    split_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_packages", x => x.id);
                    table.CheckConstraint("ck_shipment_packages_quantity_formula", "quantity_base = package_count * quantity_base_per_package");
                    table.CheckConstraint("ck_shipment_packages_quantity_positive", "package_count > 0 and quantity_base_per_package > 0 and quantity_base > 0");
                    table.CheckConstraint("ck_shipment_packages_status", "status in ('Available', 'Allocated', 'Loaded', 'Cancelled')");
                    table.CheckConstraint("ck_shipment_packages_type", "package_type in ('Case', 'Package', 'Pallet', 'Loose')");
                    table.ForeignKey(
                        name: "FK_shipment_packages_product_packagings_packaging_id",
                        column: x => x.packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_packages_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_packages_shipment_items_shipment_item_id",
                        column: x => x.shipment_item_id,
                        principalTable: "shipment_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_packages_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shipment_packages_item",
                table: "shipment_packages",
                column: "shipment_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_packages_packaging_id",
                table: "shipment_packages",
                column: "packaging_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_packages_shipment_status",
                table: "shipment_packages",
                columns: new[] { "shipment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shipment_packages_stop",
                table: "shipment_packages",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "ux_shipment_packages_active_code",
                table: "shipment_packages",
                column: "package_code",
                unique: true,
                filter: "package_code is not null and status <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_packages");
        }
    }
}
