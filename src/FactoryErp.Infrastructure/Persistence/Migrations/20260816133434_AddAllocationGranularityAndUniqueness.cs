using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationGranularityAndUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoice_item_allocations_idempotency_key",
                table: "invoice_item_allocations");

            migrationBuilder.DropIndex(
                name: "IX_invoice_item_allocations_invoice_item_id",
                table: "invoice_item_allocations");

            migrationBuilder.DropIndex(
                name: "IX_delivery_note_item_allocations_delivery_note_item_id",
                table: "delivery_note_item_allocations");

            migrationBuilder.DropIndex(
                name: "IX_delivery_note_item_allocations_idempotency_key",
                table: "delivery_note_item_allocations");

            migrationBuilder.RenameIndex(
                name: "IX_invoice_item_allocations_delivery_note_item_id_status",
                table: "invoice_item_allocations",
                newName: "ix_invoice_allocation_source_status");

            migrationBuilder.RenameIndex(
                name: "IX_delivery_note_item_allocations_sales_order_item_id_status",
                table: "delivery_note_item_allocations",
                newName: "ix_delivery_allocation_source_status");

            migrationBuilder.AddColumn<string>(
                name: "allocation_kind",
                table: "invoice_item_allocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Original");

            migrationBuilder.AddColumn<string>(
                name: "allocation_kind",
                table: "delivery_note_item_allocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Original");

            migrationBuilder.Sql("""
                UPDATE invoice_item_allocations
                SET allocation_kind = CASE
                    WHEN credited_from_id IS NOT NULL AND status = 'Active' THEN 'Reversal'
                    ELSE 'Original'
                END;

                UPDATE delivery_note_item_allocations
                SET allocation_kind = CASE
                    WHEN reversed_from_id IS NOT NULL AND status = 'Active' THEN 'Reversal'
                    ELSE 'Original'
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_invoice_allocation_idempotency_key",
                table: "invoice_item_allocations",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_allocation_target_status",
                table: "invoice_item_allocations",
                columns: new[] { "invoice_item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_invoice_allocation_active_target",
                table: "invoice_item_allocations",
                columns: new[] { "delivery_note_item_id", "invoice_item_id" },
                unique: true,
                filter: "status = 'Active' AND allocation_kind = 'Original'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoice_allocations_kind",
                table: "invoice_item_allocations",
                sql: "allocation_kind in ('Original', 'Reversal')");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_allocation_idempotency_key",
                table: "delivery_note_item_allocations",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_allocation_target_status",
                table: "delivery_note_item_allocations",
                columns: new[] { "delivery_note_item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_delivery_allocation_active_target",
                table: "delivery_note_item_allocations",
                columns: new[] { "sales_order_item_id", "delivery_note_item_id" },
                unique: true,
                filter: "status = 'Active' AND allocation_kind = 'Original'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_delivery_note_allocations_kind",
                table: "delivery_note_item_allocations",
                sql: "allocation_kind in ('Original', 'Reversal')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoice_allocation_idempotency_key",
                table: "invoice_item_allocations");

            migrationBuilder.DropIndex(
                name: "ix_invoice_allocation_target_status",
                table: "invoice_item_allocations");

            migrationBuilder.DropIndex(
                name: "ux_invoice_allocation_active_target",
                table: "invoice_item_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoice_allocations_kind",
                table: "invoice_item_allocations");

            migrationBuilder.DropIndex(
                name: "ix_delivery_allocation_idempotency_key",
                table: "delivery_note_item_allocations");

            migrationBuilder.DropIndex(
                name: "ix_delivery_allocation_target_status",
                table: "delivery_note_item_allocations");

            migrationBuilder.DropIndex(
                name: "ux_delivery_allocation_active_target",
                table: "delivery_note_item_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_delivery_note_allocations_kind",
                table: "delivery_note_item_allocations");

            migrationBuilder.DropColumn(
                name: "allocation_kind",
                table: "invoice_item_allocations");

            migrationBuilder.DropColumn(
                name: "allocation_kind",
                table: "delivery_note_item_allocations");

            migrationBuilder.RenameIndex(
                name: "ix_invoice_allocation_source_status",
                table: "invoice_item_allocations",
                newName: "IX_invoice_item_allocations_delivery_note_item_id_status");

            migrationBuilder.RenameIndex(
                name: "ix_delivery_allocation_source_status",
                table: "delivery_note_item_allocations",
                newName: "IX_delivery_note_item_allocations_sales_order_item_id_status");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_allocations_idempotency_key",
                table: "invoice_item_allocations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_allocations_invoice_item_id",
                table: "invoice_item_allocations",
                column: "invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_delivery_note_item_id",
                table: "delivery_note_item_allocations",
                column: "delivery_note_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_idempotency_key",
                table: "delivery_note_item_allocations",
                column: "idempotency_key",
                unique: true);
        }
    }
}
