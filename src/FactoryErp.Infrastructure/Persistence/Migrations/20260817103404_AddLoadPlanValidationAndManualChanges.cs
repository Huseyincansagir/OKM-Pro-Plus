using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadPlanValidationAndManualChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "load_plan_manual_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: false),
                    after_json = table.Column<string>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_plan_manual_changes", x => x.id);
                    table.CheckConstraint("ck_load_plan_manual_change_entity", "entity_id <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("ck_load_plan_manual_change_type", "change_type in ('AddLoadUnit', 'RemoveLoadUnit', 'MovePackage', 'ChangeQuantity', 'ChangeStopAllocation', 'ChangeVehicle', 'ChangeCapacity', 'Other')");
                    table.ForeignKey(
                        name: "FK_load_plan_manual_changes_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plan_manual_changes_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "load_plan_validation_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    load_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    validation_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    resolution_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_plan_validation_results", x => x.id);
                    table.CheckConstraint("ck_load_plan_validation_resolution", "resolution_status in ('Open', 'Resolved', 'Overridden', 'NotApplicable')");
                    table.CheckConstraint("ck_load_plan_validation_resolution_pair", "(resolution_status = 'Open' and resolved_by is null and resolved_at is null) or (resolution_status <> 'Open' and resolved_by is not null and resolved_at is not null and resolution_reason is not null)");
                    table.CheckConstraint("ck_load_plan_validation_severity", "severity in ('HardError', 'Warning', 'Info')");
                    table.ForeignKey(
                        name: "FK_load_plan_validation_results_load_plans_load_plan_id",
                        column: x => x.load_plan_id,
                        principalTable: "load_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_load_plan_validation_results_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_load_plan_manual_changes_actor_user_id",
                table: "load_plan_manual_changes",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_load_plan_manual_changes_entity",
                table: "load_plan_manual_changes",
                columns: new[] { "load_plan_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_load_plan_manual_changes_time",
                table: "load_plan_manual_changes",
                columns: new[] { "load_plan_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_load_plan_validation_open",
                table: "load_plan_validation_results",
                columns: new[] { "load_plan_id", "severity", "resolution_status" });

            migrationBuilder.CreateIndex(
                name: "IX_load_plan_validation_results_resolved_by",
                table: "load_plan_validation_results",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "ux_load_plan_validation_key",
                table: "load_plan_validation_results",
                columns: new[] { "load_plan_id", "validation_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "load_plan_manual_changes");

            migrationBuilder.DropTable(
                name: "load_plan_validation_results");
        }
    }
}
