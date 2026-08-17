using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleFitEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_fit_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_capacity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    candidate_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    rejection_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    reason_text = table.Column<string>(type: "text", nullable: true),
                    weight_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    volume_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    pallet_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    floor_area_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    height_ratio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    door_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    dimension_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stacking_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    axle_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stop_access_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fit_score = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    algorithm_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    input_snapshot_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    capacity_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_fit_evaluations", x => x.id);
                    table.CheckConstraint("ck_vehicle_fit_candidate_status", "candidate_status in ('Candidate', 'Recommended', 'Rejected', 'NeedsReview')");
                    table.CheckConstraint("ck_vehicle_fit_check_statuses", "door_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and dimension_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and stacking_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and axle_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and stop_access_status in ('NotChecked', 'Pass', 'Fail', 'Warning')");
                    table.CheckConstraint("ck_vehicle_fit_ratios_non_negative", "(weight_ratio is null or weight_ratio >= 0) and (volume_ratio is null or volume_ratio >= 0) and (pallet_ratio is null or pallet_ratio >= 0) and (floor_area_ratio is null or floor_area_ratio >= 0) and (height_ratio is null or height_ratio >= 0) and (fit_score is null or fit_score >= 0)");
                    table.ForeignKey(
                        name: "FK_vehicle_fit_evaluations_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_fit_evaluations_vehicle_capacities_vehicle_capacity~",
                        column: x => x.vehicle_capacity_id,
                        principalTable: "vehicle_capacities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_fit_evaluations_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fit_evaluations_vehicle_capacity_id",
                table: "vehicle_fit_evaluations",
                column: "vehicle_capacity_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_fit_plan_status_score",
                table: "vehicle_fit_evaluations",
                columns: new[] { "load_plan_id", "candidate_status", "fit_score" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_fit_vehicle_evaluated",
                table: "vehicle_fit_evaluations",
                columns: new[] { "vehicle_id", "evaluated_at" });

            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_vehicle_fit_snapshot_candidate ON vehicle_fit_evaluations (load_plan_id, vehicle_id, COALESCE(vehicle_capacity_id, '00000000-0000-0000-0000-000000000000'::uuid), input_snapshot_hash);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_vehicle_fit_snapshot_candidate;");
            migrationBuilder.DropTable(
                name: "vehicle_fit_evaluations");
        }
    }
}
