using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPackagingAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planned_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    completed_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_orders", x => x.id);
                    table.CheckConstraint("ck_production_orders_completed_valid", "completed_qty_base >= 0 and completed_qty_base <= planned_qty_base");
                    table.CheckConstraint("ck_production_orders_planned_positive", "planned_qty_base > 0");
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    source_entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_quantity_positive", "quantity_base > 0");
                });

            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    reserved_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks", x => x.id);
                    table.CheckConstraint("ck_stocks_on_hand_non_negative", "on_hand_qty_base >= 0");
                    table.CheckConstraint("ck_stocks_reserved_non_negative", "reserved_qty_base >= 0");
                    table.CheckConstraint("ck_stocks_reserved_not_above_on_hand", "reserved_qty_base <= on_hand_qty_base");
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    dimension = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    decimal_scale = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units_of_measure", x => x.id);
                    table.CheckConstraint("ck_units_of_measure_decimal_scale", "decimal_scale between 0 and 6");
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "production_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_records", x => x.id);
                    table.CheckConstraint("ck_production_records_quantity_positive", "quantity_base > 0");
                    table.ForeignKey(
                        name: "FK_production_records_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    size_label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_product_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_units_of_measure_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "units_of_measure",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_warehouse_locations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_packagings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    units_per_parent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    quantity_in_base_uom = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: false),
                    allow_partial = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_packagings", x => x.id);
                    table.CheckConstraint("ck_product_packagings_quantity_positive", "quantity_in_base_uom > 0");
                    table.CheckConstraint("ck_product_packagings_units_positive", "units_per_parent > 0");
                    table.ForeignKey(
                        name: "FK_product_packagings_product_packagings_parent_packaging_id",
                        column: x => x.parent_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_packagings_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_barcodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    barcode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_barcodes_product_packagings_packaging_id",
                        column: x => x.packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_barcodes_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_barcode",
                table: "product_barcodes",
                column: "barcode",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_packaging_id",
                table: "product_barcodes",
                column: "packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_product_id",
                table: "product_barcodes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_code",
                table: "product_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_slug",
                table: "product_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id_sort_order",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_product_packagings_parent_packaging_id",
                table: "product_packagings",
                column: "parent_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_packagings_product_id_is_sellable_effective_to",
                table: "product_packagings",
                columns: new[] { "product_id", "is_sellable", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_product_packagings_product_id_level_effective_from",
                table: "product_packagings",
                columns: new[] { "product_id", "level", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_status_product_id",
                table: "production_orders",
                columns: new[] { "status", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_records_product_id_completed_at",
                table: "production_records",
                columns: new[] { "product_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_production_records_production_order_id",
                table: "production_records",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_base_uom_id",
                table: "products",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_code",
                table: "products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_is_active_is_public_category_id",
                table: "products",
                columns: new[] { "is_active", "is_public", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_products_slug",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_id_warehouse_id_created_at",
                table: "stock_movements",
                columns: new[] { "product_id", "warehouse_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_source_entity_type_source_entity_id",
                table: "stock_movements",
                columns: new[] { "source_entity_type", "source_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_product_id_warehouse_id_location_id",
                table: "stocks",
                columns: new[] { "product_id", "warehouse_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_of_measure_code",
                table: "units_of_measure",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_locations_warehouse_id_code",
                table: "warehouse_locations",
                columns: new[] { "warehouse_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_code",
                table: "warehouses",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_barcodes");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "production_records");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "stocks");

            migrationBuilder.DropTable(
                name: "warehouse_locations");

            migrationBuilder.DropTable(
                name: "product_packagings");

            migrationBuilder.DropTable(
                name: "production_orders");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropTable(
                name: "units_of_measure");
        }
    }
}
