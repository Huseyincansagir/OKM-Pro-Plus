using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadPlanAndUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "load_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_plan_version = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    replanned_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_capacity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    feasibility_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    algorithm_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    algorithm_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    parameter_set = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    input_snapshot_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    capacity_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    utilization_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    validation_summary = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    locked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_plans", x => x.id);
                    table.CheckConstraint("ck_load_plans_approval_pair", "(approved_by is null and approved_at is null) or (approved_by is not null and approved_at is not null)");
                    table.CheckConstraint("ck_load_plans_feasibility", "feasibility_status in ('Infeasible', 'FeasibleWithWarnings', 'Feasible')");
                    table.CheckConstraint("ck_load_plans_lock_pair", "(locked_by is null and locked_at is null) or (locked_by is not null and locked_at is not null)");
                    table.CheckConstraint("ck_load_plans_locked_requirements", "status <> 'Locked' or (vehicle_id is not null and vehicle_capacity_id is not null and input_snapshot_hash is not null and locked_by is not null)");
                    table.CheckConstraint("ck_load_plans_status", "status in ('Draft', 'Proposed', 'Validating', 'Valid', 'NeedsReview', 'Locked', 'Superseded')");
                    table.CheckConstraint("ck_load_plans_version_positive", "version > 0 and route_plan_version > 0");
                    table.ForeignKey(
                        name: "FK_load_plans_load_plans_replanned_from_id",
                        column: x => x.replanned_from_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_route_plans_route_plan_id",
                        column: x => x.route_plan_id,
                        principalTable: "route_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_users_locked_by",
                        column: x => x.locked_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_vehicle_capacities_vehicle_capacity_id",
                        column: x => x.vehicle_capacity_id,
                        principalTable: "vehicle_capacities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plans_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "load_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    unit_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    pallet_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_mixed = table.Column<bool>(type: "boolean", nullable: false),
                    length_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    width_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    height_mm = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    tare_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    volume_m3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    max_stack_count = table.Column<int>(type: "integer", nullable: true),
                    placement_zone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    unloading_priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_units", x => x.id);
                    table.CheckConstraint("ck_load_units_dimensions", "length_mm > 0 and width_mm > 0 and height_mm > 0");
                    table.CheckConstraint("ck_load_units_priority", "unloading_priority > 0");
                    table.CheckConstraint("ck_load_units_stack_count", "max_stack_count is null or max_stack_count >= 1");
                    table.CheckConstraint("ck_load_units_status", "status in ('Draft', 'Validated', 'Locked', 'Loaded', 'Cancelled')");
                    table.CheckConstraint("ck_load_units_type", "unit_type in ('Pallet', 'Cage', 'CartonGroup', 'Loose')");
                    table.CheckConstraint("ck_load_units_volume", "volume_m3 > 0");
                    table.CheckConstraint("ck_load_units_weight", "gross_weight_kg >= tare_weight_kg and tare_weight_kg >= 0");
                    table.ForeignKey(
                        name: "FK_load_units_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_units_pallet_types_pallet_type_id",
                        column: x => x.pallet_type_id,
                        principalTable: "pallet_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "load_unit_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_weight_kg = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    volume_m3 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    allocation_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_unit_items", x => x.id);
                    table.CheckConstraint("ck_load_unit_items_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_load_unit_items_volume_non_negative", "volume_m3 >= 0");
                    table.CheckConstraint("ck_load_unit_items_weight_non_negative", "gross_weight_kg >= 0");
                    table.ForeignKey(
                        name: "FK_load_unit_items_load_units_load_unit_id",
                        column: x => x.load_unit_id,
                        principalTable: "load_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_unit_items_shipment_items_shipment_item_id",
                        column: x => x.shipment_item_id,
                        principalTable: "shipment_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_unit_items_shipment_packages_shipment_package_id",
                        column: x => x.shipment_package_id,
                        principalTable: "shipment_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "load_unit_stop_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_unit_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sequence_no = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_unit_stop_allocations", x => x.id);
                    table.CheckConstraint("ck_load_unit_stop_allocations_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_load_unit_stop_allocations_sequence_positive", "sequence_no > 0");
                    table.ForeignKey(
                        name: "FK_load_unit_stop_allocations_load_unit_items_load_unit_item_id",
                        column: x => x.load_unit_item_id,
                        principalTable: "load_unit_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_unit_stop_allocations_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_load_plans_approved_by",
                table: "load_plans",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_load_plans_locked_by",
                table: "load_plans",
                column: "locked_by");

            migrationBuilder.CreateIndex(
                name: "IX_load_plans_replanned_from_id",
                table: "load_plans",
                column: "replanned_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_plans_route",
                table: "load_plans",
                columns: new[] { "route_plan_id", "route_plan_version", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_load_plans_vehicle",
                table: "load_plans",
                columns: new[] { "vehicle_id", "status" },
                filter: "vehicle_id is not null");

            migrationBuilder.CreateIndex(
                name: "IX_load_plans_vehicle_capacity_id",
                table: "load_plans",
                column: "vehicle_capacity_id");

            migrationBuilder.CreateIndex(
                name: "ux_load_plans_shipment_version",
                table: "load_plans",
                columns: new[] { "shipment_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_load_unit_items_shipment_item_id",
                table: "load_unit_items",
                column: "shipment_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_unit_items_unit",
                table: "load_unit_items",
                column: "load_unit_id");

            migrationBuilder.CreateIndex(
                name: "ux_active_package_load_unit",
                table: "load_unit_items",
                column: "shipment_package_id",
                unique: true,
                filter: "quantity_base > 0");

            migrationBuilder.CreateIndex(
                name: "ix_load_unit_stop_route_order",
                table: "load_unit_stop_allocations",
                columns: new[] { "route_stop_id", "sequence_no" });

            migrationBuilder.CreateIndex(
                name: "ux_load_unit_stop_allocation",
                table: "load_unit_stop_allocations",
                columns: new[] { "load_unit_item_id", "route_stop_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_load_units_pallet_type_id",
                table: "load_units",
                column: "pallet_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_units_plan_priority",
                table: "load_units",
                columns: new[] { "load_plan_id", "unloading_priority", "unit_code" });

            migrationBuilder.CreateIndex(
                name: "ux_load_units_plan_code",
                table: "load_units",
                columns: new[] { "load_plan_id", "unit_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "load_unit_stop_allocations");

            migrationBuilder.DropTable(
                name: "load_unit_items");

            migrationBuilder.DropTable(
                name: "load_units");

            migrationBuilder.DropTable(
                name: "load_plans");
        }
    }
}
