using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchRunsAndRouteExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_route_stops_status",
                table: "route_stops");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actual_departure_at",
                table: "route_stops",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "skipped_at",
                table: "route_stops",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dispatch_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    planned_departure_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_departed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    dispatched_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    exception_reason = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispatch_runs", x => x.id);
                    table.CheckConstraint("ck_dispatch_runs_cancelled_pair", "(status <> 'Cancelled' and cancelled_at is null and cancelled_by is null) or (status = 'Cancelled' and cancelled_at is not null and cancelled_by is not null and nullif(btrim(exception_reason), '') is not null)");
                    table.CheckConstraint("ck_dispatch_runs_completed_pair", "(status <> 'Completed' and completed_at is null and completed_by is null) or (status = 'Completed' and completed_at is not null and completed_by is not null)");
                    table.CheckConstraint("ck_dispatch_runs_departed_pair", "status in ('Prepared', 'Dispatched', 'Cancelled') or actual_departed_at is not null");
                    table.CheckConstraint("ck_dispatch_runs_status", "status in ('Prepared', 'Dispatched', 'InTransit', 'Completed', 'Cancelled')");
                    table.CheckConstraint("ck_dispatch_runs_time_order", "completed_at is null or actual_departed_at is null or completed_at >= actual_departed_at");
                    table.ForeignKey(
                        name: "FK_dispatch_runs_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_route_plans_route_plan_id",
                        column: x => x.route_plan_id,
                        principalTable: "route_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_users_completed_by",
                        column: x => x.completed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_users_dispatched_by",
                        column: x => x.dispatched_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispatch_runs_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "route_execution_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispatch_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sequence_no = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    location_text = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_snapshot = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_execution_events", x => x.id);
                    table.CheckConstraint("ck_route_execution_events_location", "(latitude is null and longitude is null) or (latitude between -90 and 90 and longitude between -180 and 180)");
                    table.CheckConstraint("ck_route_execution_events_reason", "event_type not in ('SkippedStop', 'Cancelled') or nullif(btrim(reason), '') is not null");
                    table.CheckConstraint("ck_route_execution_events_sequence", "sequence_no > 0");
                    table.CheckConstraint("ck_route_execution_events_stop_pair", "event_type in ('Departed', 'RouteCompleted', 'Cancelled') or route_stop_id is not null");
                    table.CheckConstraint("ck_route_execution_events_type", "event_type in ('Departed', 'ArrivedAtStop', 'DepartedStop', 'SkippedStop', 'RouteCompleted', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_route_execution_events_dispatch_runs_dispatch_run_id",
                        column: x => x.dispatch_run_id,
                        principalTable: "dispatch_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_execution_events_route_plans_route_plan_id",
                        column: x => x.route_plan_id,
                        principalTable: "route_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_execution_events_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_execution_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_route_stops_execution_time_order",
                table: "route_stops",
                sql: "actual_departure_at is null or actual_arrival_at is null or actual_departure_at >= actual_arrival_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_route_stops_skipped_reason",
                table: "route_stops",
                sql: "status <> 'Skipped' or nullif(btrim(exception_reason), '') is not null");

            migrationBuilder.AddCheckConstraint(
                name: "ck_route_stops_status",
                table: "route_stops",
                sql: "status in ('Pending', 'Arrived', 'Departed', 'InProgress', 'Delivered', 'Partial', 'Failed', 'Skipped')");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_runs_board",
                table: "dispatch_runs",
                columns: new[] { "status", "planned_departure_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_runs_cancelled_by",
                table: "dispatch_runs",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_runs_completed_by",
                table: "dispatch_runs",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_runs_created_by",
                table: "dispatch_runs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_runs_dispatched_by",
                table: "dispatch_runs",
                column: "dispatched_by");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_runs_load_plan_id",
                table: "dispatch_runs",
                column: "load_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_runs_shipment_history",
                table: "dispatch_runs",
                columns: new[] { "shipment_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_dispatch_runs_vehicle_history",
                table: "dispatch_runs",
                columns: new[] { "vehicle_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_dispatch_runs_active_driver",
                table: "dispatch_runs",
                column: "driver_id",
                unique: true,
                filter: "status in ('Prepared', 'Dispatched', 'InTransit')");

            migrationBuilder.CreateIndex(
                name: "ux_dispatch_runs_active_route_plan",
                table: "dispatch_runs",
                column: "route_plan_id",
                unique: true,
                filter: "status in ('Prepared', 'Dispatched', 'InTransit')");

            migrationBuilder.CreateIndex(
                name: "ux_dispatch_runs_active_shipment",
                table: "dispatch_runs",
                column: "shipment_id",
                unique: true,
                filter: "status in ('Prepared', 'Dispatched', 'InTransit')");

            migrationBuilder.CreateIndex(
                name: "ux_dispatch_runs_active_vehicle",
                table: "dispatch_runs",
                column: "vehicle_id",
                unique: true,
                filter: "status in ('Prepared', 'Dispatched', 'InTransit')");

            migrationBuilder.CreateIndex(
                name: "IX_route_execution_events_actor_id",
                table: "route_execution_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_execution_events_route_plan_id",
                table: "route_execution_events",
                column: "route_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_execution_events_stop",
                table: "route_execution_events",
                columns: new[] { "route_stop_id", "occurred_at", "id" },
                filter: "route_stop_id is not null");

            migrationBuilder.CreateIndex(
                name: "ix_route_execution_events_timeline",
                table: "route_execution_events",
                columns: new[] { "dispatch_run_id", "sequence_no", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_route_execution_events_type_time",
                table: "route_execution_events",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_route_execution_events_idempotency",
                table: "route_execution_events",
                columns: new[] { "dispatch_run_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_route_execution_events_sequence",
                table: "route_execution_events",
                columns: new[] { "dispatch_run_id", "sequence_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_execution_events");

            migrationBuilder.DropTable(
                name: "dispatch_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_route_stops_execution_time_order",
                table: "route_stops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_route_stops_skipped_reason",
                table: "route_stops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_route_stops_status",
                table: "route_stops");

            migrationBuilder.DropColumn(
                name: "actual_departure_at",
                table: "route_stops");

            migrationBuilder.DropColumn(
                name: "skipped_at",
                table: "route_stops");

            migrationBuilder.AddCheckConstraint(
                name: "ck_route_stops_status",
                table: "route_stops",
                sql: "status in ('Pending', 'InProgress', 'Delivered', 'Partial', 'Failed', 'Skipped')");
        }
    }
}
