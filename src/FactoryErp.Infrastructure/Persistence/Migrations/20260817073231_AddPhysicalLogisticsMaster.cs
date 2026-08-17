using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalLogisticsMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packaging_physical_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    units_per_package = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    length_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    width_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    height_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    net_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    tare_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    volume_m3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: false),
                    max_stack_count = table.Column<int>(type: "integer", nullable: true),
                    max_load_above_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    keep_upright = table.Column<bool>(type: "boolean", nullable: false),
                    is_fragile = table.Column<bool>(type: "boolean", nullable: false),
                    compatibility_group = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    incompatible_groups = table.Column<string>(type: "jsonb", nullable: false),
                    allowed_orientations = table.Column<string>(type: "jsonb", nullable: false),
                    physical_policy_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packaging_physical_profiles", x => x.id);
                    table.CheckConstraint("ck_packaging_physical_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
                    table.CheckConstraint("ck_packaging_physical_effective_range", "effective_to is null or effective_to > effective_from");
                    table.CheckConstraint("ck_packaging_physical_gross_consistent", "gross_weight_kg is null or net_weight_kg is null or gross_weight_kg >= net_weight_kg + tare_weight_kg");
                    table.CheckConstraint("ck_packaging_physical_stack_rules", "max_stack_count is null or max_stack_count >= 1");
                    table.CheckConstraint("ck_packaging_physical_units_positive", "units_per_package > 0");
                    table.CheckConstraint("ck_packaging_physical_weights_nonnegative", "(net_weight_kg is null or net_weight_kg >= 0) and tare_weight_kg >= 0 and (gross_weight_kg is null or gross_weight_kg >= 0)");
                    table.ForeignKey(
                        name: "FK_packaging_physical_profiles_product_packagings_packaging_id",
                        column: x => x.packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pallet_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    length_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    width_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    height_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    tare_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    max_gross_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    max_payload_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    max_load_height_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    max_stack_count = table.Column<int>(type: "integer", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    policy_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pallet_types", x => x.id);
                    table.CheckConstraint("ck_pallet_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
                    table.CheckConstraint("ck_pallet_payload_not_over_gross", "max_payload_kg is null or max_gross_weight_kg is null or max_payload_kg <= max_gross_weight_kg");
                    table.CheckConstraint("ck_pallet_stack_rules", "max_stack_count is null or max_stack_count >= 1");
                    table.CheckConstraint("ck_pallet_weights_nonnegative", "tare_weight_kg >= 0 and (max_gross_weight_kg is null or max_gross_weight_kg >= 0) and (max_payload_kg is null or max_payload_kg >= 0)");
                });

            migrationBuilder.CreateTable(
                name: "product_physical_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    length_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    width_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    height_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    net_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    volume_m3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: false),
                    max_stack_count = table.Column<int>(type: "integer", nullable: true),
                    max_load_above_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    keep_upright = table.Column<bool>(type: "boolean", nullable: false),
                    is_fragile = table.Column<bool>(type: "boolean", nullable: false),
                    compatibility_group = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    incompatible_groups = table.Column<string>(type: "jsonb", nullable: false),
                    allowed_orientations = table.Column<string>(type: "jsonb", nullable: false),
                    physical_policy_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_physical_profiles", x => x.id);
                    table.CheckConstraint("ck_product_physical_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
                    table.CheckConstraint("ck_product_physical_effective_range", "effective_to is null or effective_to > effective_from");
                    table.CheckConstraint("ck_product_physical_load_above", "max_load_above_kg is null or max_load_above_kg >= 0");
                    table.CheckConstraint("ck_product_physical_stack_rules", "max_stack_count is null or max_stack_count >= 1");
                    table.CheckConstraint("ck_product_physical_volume_positive", "volume_m3 is null or volume_m3 > 0");
                    table.CheckConstraint("ck_product_physical_weight_nonnegative", "net_weight_kg >= 0");
                    table.ForeignKey(
                        name: "FK_product_physical_profiles_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_capacity_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_capacity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    length_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    width_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    max_load_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    access_side = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    sequence_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_capacity_zones", x => x.id);
                    table.CheckConstraint("ck_vehicle_capacity_zone_dimensions_positive", "length_mm > 0 and width_mm > 0");
                    table.CheckConstraint("ck_vehicle_capacity_zone_load_nonnegative", "max_load_kg is null or max_load_kg >= 0");
                    table.CheckConstraint("ck_vehicle_capacity_zone_sequence_positive", "sequence_no >= 1");
                    table.ForeignKey(
                        name: "FK_vehicle_capacity_zones_vehicle_capacities_vehicle_capacity_~",
                        column: x => x.vehicle_capacity_id,
                        principalTable: "vehicle_capacities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_capacity_pallet_types",
                columns: table => new
                {
                    vehicle_capacity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pallet_type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_capacity_pallet_types", x => new { x.vehicle_capacity_id, x.pallet_type_id });
                    table.ForeignKey(
                        name: "FK_vehicle_capacity_pallet_types_pallet_types_pallet_type_id",
                        column: x => x.pallet_type_id,
                        principalTable: "pallet_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_capacity_pallet_types_vehicle_capacities_vehicle_ca~",
                        column: x => x.vehicle_capacity_id,
                        principalTable: "vehicle_capacities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_packaging_physical_packaging",
                table: "packaging_physical_profiles",
                column: "packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_packaging_physical_profiles_packaging_id_effective_from",
                table: "packaging_physical_profiles",
                columns: new[] { "packaging_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pallet_types_code",
                table: "pallet_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_physical_product",
                table: "product_physical_profiles",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_physical_profiles_product_id_effective_from",
                table: "product_physical_profiles",
                columns: new[] { "product_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_capacity_pallet_types_pallet_type_id",
                table: "vehicle_capacity_pallet_types",
                column: "pallet_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_capacity_zones_vehicle_capacity_id_sequence_no",
                table: "vehicle_capacity_zones",
                columns: new[] { "vehicle_capacity_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_capacity_zones_vehicle_capacity_id_zone_code",
                table: "vehicle_capacity_zones",
                columns: new[] { "vehicle_capacity_id", "zone_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "packaging_physical_profiles");

            migrationBuilder.DropTable(
                name: "product_physical_profiles");

            migrationBuilder.DropTable(
                name: "vehicle_capacity_pallet_types");

            migrationBuilder.DropTable(
                name: "vehicle_capacity_zones");

            migrationBuilder.DropTable(
                name: "pallet_types");
        }
    }
}
