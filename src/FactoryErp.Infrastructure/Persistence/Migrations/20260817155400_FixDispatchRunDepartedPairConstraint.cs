using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDispatchRunDepartedPairConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_dispatch_runs_departed_pair",
                table: "dispatch_runs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dispatch_runs_departed_pair",
                table: "dispatch_runs",
                sql: "status in ('Prepared', 'Dispatched', 'Cancelled') or actual_departed_at is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_dispatch_runs_departed_pair",
                table: "dispatch_runs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dispatch_runs_departed_pair",
                table: "dispatch_runs",
                sql: "status in ('Prepared', 'Cancelled') or actual_departed_at is not null");
        }
    }
}
