using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceQuoteToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_quote_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_source_quote_id",
                table: "sales_orders",
                column: "source_quote_id",
                unique: true,
                filter: "source_quote_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_orders_quotes_source_quote_id",
                table: "sales_orders",
                column: "source_quote_id",
                principalTable: "quotes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sales_orders_quotes_source_quote_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_source_quote_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "source_quote_id",
                table: "sales_orders");
        }
    }
}
