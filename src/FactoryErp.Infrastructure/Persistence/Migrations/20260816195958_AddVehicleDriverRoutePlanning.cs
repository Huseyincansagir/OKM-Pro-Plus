using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDriverRoutePlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    license_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    license_expiry = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.id);
                    table.CheckConstraint("ck_drivers_status", "status in ('Active', 'Suspended', 'Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                    table.CheckConstraint("ck_shipments_status", "status in ('Preparing', 'Ready', 'Loaded', 'InTransit', 'PartiallyDelivered', 'Delivered', 'Exception', 'Returned')");
                    table.ForeignKey(
                        name: "FK_shipments_delivery_notes_delivery_note_id",
                        column: x => x.delivery_note_id,
                        principalTable: "delivery_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_items", x => x.id);
                    table.CheckConstraint("ck_shipment_items_quantity_positive", "quantity_base > 0");
                    table.ForeignKey(
                        name: "FK_shipment_items_delivery_note_items_delivery_note_item_id",
                        column: x => x.delivery_note_item_id,
                        principalTable: "delivery_note_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_items_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_capacities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    max_gross_weight = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    tare_weight = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    max_usable_volume = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    max_pallet_count = table.Column<int>(type: "integer", nullable: false),
                    max_load_height = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    capacity_policy_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_capacities", x => x.id);
                    table.CheckConstraint("ck_vehicle_capacities_effective_range", "effective_to is null or effective_to > effective_from");
                    table.CheckConstraint("ck_vehicle_capacities_limits", "max_usable_volume > 0 and max_pallet_count > 0 and max_load_height > 0");
                    table.CheckConstraint("ck_vehicle_capacities_weight", "max_gross_weight > 0 and tare_weight >= 0 and tare_weight < max_gross_weight");
                    table.ForeignKey(
                        name: "FK_vehicle_capacities_vehicle_types_vehicle_type_id",
                        column: x => x.vehicle_type_id,
                        principalTable: "vehicle_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    maintenance_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    current_route_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_known_location_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    last_status_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_status", "status in ('Available', 'Assigned', 'Loading', 'InTransit', 'Maintenance', 'OutOfService')");
                    table.ForeignKey(
                        name: "FK_vehicles_vehicle_types_vehicle_type_id",
                        column: x => x.vehicle_type_id,
                        principalTable: "vehicle_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    replanned_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_start_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    planned_end_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_plans", x => x.id);
                    table.CheckConstraint("ck_route_plans_status", "status in ('Draft', 'Planned', 'Locked', 'InProgress', 'Completed', 'Exception', 'Superseded')");
                    table.CheckConstraint("ck_route_plans_valid_time", "planned_start_at is null or planned_end_at is null or planned_end_at > planned_start_at");
                    table.CheckConstraint("ck_route_plans_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_route_plans_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_plans_route_plans_replanned_from_id",
                        column: x => x.replanned_from_id,
                        principalTable: "route_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_plans_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_plans_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_no = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    planned_arrival_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_arrival_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    exception_reason = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => x.id);
                    table.CheckConstraint("ck_route_stops_sequence_positive", "sequence_no > 0");
                    table.CheckConstraint("ck_route_stops_status", "status in ('Pending', 'InProgress', 'Delivered', 'Partial', 'Failed', 'Skipped')");
                    table.ForeignKey(
                        name: "FK_route_stops_customer_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "customer_addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_stops_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_stops_route_plans_route_plan_id",
                        column: x => x.route_plan_id,
                        principalTable: "route_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drivers_is_active_license_expiry",
                table: "drivers",
                columns: new[] { "is_active", "license_expiry" });

            migrationBuilder.CreateIndex(
                name: "IX_drivers_license_number",
                table: "drivers",
                column: "license_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_plans_driver_id_planned_start_at_planned_end_at",
                table: "route_plans",
                columns: new[] { "driver_id", "planned_start_at", "planned_end_at" },
                filter: "driver_id is not null");

            migrationBuilder.CreateIndex(
                name: "IX_route_plans_replanned_from_id",
                table: "route_plans",
                column: "replanned_from_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_plans_shipment_id_version",
                table: "route_plans",
                columns: new[] { "shipment_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_plans_vehicle_id_planned_start_at_planned_end_at",
                table: "route_plans",
                columns: new[] { "vehicle_id", "planned_start_at", "planned_end_at" },
                filter: "vehicle_id is not null");

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_address_id",
                table: "route_stops",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_customer_id_status_planned_arrival_at",
                table: "route_stops",
                columns: new[] { "customer_id", "status", "planned_arrival_at" });

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_route_plan_id_sequence_no",
                table: "route_stops",
                columns: new[] { "route_plan_id", "sequence_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_delivery_note_item_id",
                table: "shipment_items",
                column: "delivery_note_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_product_id",
                table: "shipment_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_shipment_id_delivery_note_item_id",
                table: "shipment_items",
                columns: new[] { "shipment_id", "delivery_note_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_delivery_note_id",
                table: "shipments",
                column: "delivery_note_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_status_created_at",
                table: "shipments",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_capacities_vehicle_type_id_effective_from",
                table: "vehicle_capacities",
                columns: new[] { "vehicle_type_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_capacities_vehicle_type_id_effective_from_effective~",
                table: "vehicle_capacities",
                columns: new[] { "vehicle_type_id", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_types_code",
                table: "vehicle_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_plate_number",
                table: "vehicles",
                column: "plate_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_status_maintenance_until",
                table: "vehicles",
                columns: new[] { "status", "maintenance_until" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_vehicle_type_id",
                table: "vehicles",
                column: "vehicle_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_stops");

            migrationBuilder.DropTable(
                name: "shipment_items");

            migrationBuilder.DropTable(
                name: "vehicle_capacities");

            migrationBuilder.DropTable(
                name: "route_plans");

            migrationBuilder.DropTable(
                name: "drivers");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_types");
        }
    }
}
