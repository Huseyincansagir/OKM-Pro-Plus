using System;
using FactoryErp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FactoryErpDbContext))]
    [Migration("20260819180000_AddDeliveryProofCountsEmployees")]
    public partial class AddDeliveryProofCountsEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at",
                table: "route_stops",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_recipient",
                table: "route_stops",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_note",
                table: "route_stops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE route_execution_events DROP CONSTRAINT IF EXISTS ck_route_execution_events_type;
                ALTER TABLE route_execution_events ADD CONSTRAINT ck_route_execution_events_type
                    CHECK (event_type in ('Departed', 'ArrivedAtStop', 'DeliveredStop', 'DepartedStop', 'SkippedStop', 'RouteCompleted', 'Cancelled'));
                """);

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hired_on = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.CheckConstraint("ck_employees_status", "status in ('Active', 'Inactive')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_code",
                table: "employees",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "stock_counts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_counts", x => x.id);
                    table.CheckConstraint("ck_stock_counts_status", "status in ('Draft', 'Completed')");
                    table.ForeignKey(
                        name: "FK_stock_counts_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_counts_warehouse_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "warehouse_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_counts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_document_number",
                table: "stock_counts",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateTable(
                name: "stock_count_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_count_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counted_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    system_on_hand_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    variance_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_count_items", x => x.id);
                    table.CheckConstraint("ck_stock_count_items_counted_non_negative", "counted_qty_base >= 0");
                    table.ForeignKey(
                        name: "FK_stock_count_items_stock_counts_stock_count_id",
                        column: x => x.stock_count_id,
                        principalTable: "stock_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_count_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_items_stock_count_id_product_id",
                table: "stock_count_items",
                columns: new[] { "stock_count_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "stock_count_items");
            migrationBuilder.DropTable(name: "stock_counts");
            migrationBuilder.DropTable(name: "employees");
            migrationBuilder.DropColumn(name: "delivered_at", table: "route_stops");
            migrationBuilder.DropColumn(name: "proof_recipient", table: "route_stops");
            migrationBuilder.DropColumn(name: "proof_note", table: "route_stops");
            migrationBuilder.Sql("""
                ALTER TABLE route_execution_events DROP CONSTRAINT IF EXISTS ck_route_execution_events_type;
                ALTER TABLE route_execution_events ADD CONSTRAINT ck_route_execution_events_type
                    CHECK (event_type in ('Departed', 'ArrivedAtStop', 'DepartedStop', 'SkippedStop', 'RouteCompleted', 'Cancelled'));
                """);
        }
    }
}
