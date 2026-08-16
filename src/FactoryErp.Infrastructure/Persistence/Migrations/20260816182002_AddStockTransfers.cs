using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    view_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transfers", x => x.id);
                    table.CheckConstraint("ck_stock_transfers_entered_quantity_positive", "entered_quantity > 0");
                    table.CheckConstraint("ck_stock_transfers_quantity_positive", "quantity_base > 0");
                    table.ForeignKey(
                        name: "FK_stock_transfers_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouse_locations_source_location_id",
                        column: x => x.source_location_id,
                        principalTable: "warehouse_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouse_locations_target_location_id",
                        column: x => x.target_location_id,
                        principalTable: "warehouse_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_source_warehouse_id",
                        column: x => x.source_warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_target_warehouse_id",
                        column: x => x.target_warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_entered_packaging_id",
                table: "stock_transfers",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_product_id_source_warehouse_id_target_wareh~",
                table: "stock_transfers",
                columns: new[] { "product_id", "source_warehouse_id", "target_warehouse_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_source_location_id",
                table: "stock_transfers",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_source_warehouse_id",
                table: "stock_transfers",
                column: "source_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_status_created_at",
                table: "stock_transfers",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_target_location_id",
                table: "stock_transfers",
                column: "target_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_target_warehouse_id",
                table: "stock_transfers",
                column: "target_warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_transfers");
        }
    }
}
