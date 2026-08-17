using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "load_verification_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_by = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completion_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_verification_sessions", x => x.id);
                    table.CheckConstraint("ck_load_verification_session_completion_pair", "(status in ('Completed', 'Discrepancy', 'Cancelled') and completed_by is not null and completed_at is not null) or (status in ('Draft', 'InProgress') and completed_by is null and completed_at is null)");
                    table.CheckConstraint("ck_load_verification_session_discrepancy_reason", "(status <> 'Discrepancy') or (completion_reason is not null and length(btrim(completion_reason)) > 0)");
                    table.CheckConstraint("ck_load_verification_session_status", "status in ('Draft', 'InProgress', 'Completed', 'Discrepancy', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_load_verification_sessions_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_sessions_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_sessions_users_completed_by",
                        column: x => x.completed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_sessions_users_started_by",
                        column: x => x.started_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "load_verification_scans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_load_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_load_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    barcode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    scan_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason_text = table.Column<string>(type: "text", nullable: true),
                    scanned_by = table.Column<Guid>(type: "uuid", nullable: false),
                    scanned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_verification_scans", x => x.id);
                    table.CheckConstraint("ck_load_verification_scan_accepted_package", "status <> 'Accepted' or shipment_package_id is not null");
                    table.CheckConstraint("ck_load_verification_scan_barcode", "length(btrim(barcode)) > 0");
                    table.CheckConstraint("ck_load_verification_scan_keys", "length(btrim(idempotency_key)) > 0 and length(btrim(correlation_id)) > 0");
                    table.CheckConstraint("ck_load_verification_scan_mode", "scan_mode in ('Pallet', 'Case', 'Package', 'BaseUnit')");
                    table.CheckConstraint("ck_load_verification_scan_quantity", "quantity_base > 0");
                    table.CheckConstraint("ck_load_verification_scan_status", "status in ('Accepted', 'Duplicate', 'Unexpected', 'WrongUnit', 'CancelledPackage', 'Discrepancy')");
                    table.ForeignKey(
                        name: "FK_load_verification_scans_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_load_units_actual_load_unit_id",
                        column: x => x.actual_load_unit_id,
                        principalTable: "load_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_load_units_expected_load_unit_id",
                        column: x => x.expected_load_unit_id,
                        principalTable: "load_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_load_verification_sessions_session_~",
                        column: x => x.session_id,
                        principalTable: "load_verification_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_shipment_packages_shipment_package_~",
                        column: x => x.shipment_package_id,
                        principalTable: "shipment_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_verification_scans_users_scanned_by",
                        column: x => x.scanned_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_scans_actual_load_unit_id",
                table: "load_verification_scans",
                column: "actual_load_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_scans_expected_load_unit_id",
                table: "load_verification_scans",
                column: "expected_load_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_verification_scans_plan_barcode_time",
                table: "load_verification_scans",
                columns: new[] { "load_plan_id", "barcode", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_scans_scanned_by",
                table: "load_verification_scans",
                column: "scanned_by");

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_scans_shipment_id",
                table: "load_verification_scans",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_scans_shipment_package_id",
                table: "load_verification_scans",
                column: "shipment_package_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_verification_scans_time",
                table: "load_verification_scans",
                columns: new[] { "session_id", "scanned_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_load_verification_accepted_package",
                table: "load_verification_scans",
                columns: new[] { "session_id", "shipment_package_id" },
                unique: true,
                filter: "status = 'Accepted' and shipment_package_id is not null");

            migrationBuilder.CreateIndex(
                name: "ux_load_verification_scan_idempotency",
                table: "load_verification_scans",
                columns: new[] { "session_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_sessions_completed_by",
                table: "load_verification_sessions",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "ix_load_verification_sessions_shipment_status",
                table: "load_verification_sessions",
                columns: new[] { "shipment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_load_verification_sessions_started_by",
                table: "load_verification_sessions",
                column: "started_by");

            migrationBuilder.CreateIndex(
                name: "ux_load_verification_active_session",
                table: "load_verification_sessions",
                column: "load_plan_id",
                unique: true,
                filter: "status in ('Draft', 'InProgress')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "load_verification_scans");

            migrationBuilder.DropTable(
                name: "load_verification_sessions");
        }
    }
}
