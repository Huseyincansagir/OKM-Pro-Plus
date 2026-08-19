using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_net = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_gross = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.id);
                    table.CheckConstraint("ck_quotes_status", "status in ('Draft', 'Issued')");
                    table.CheckConstraint("ck_quotes_totals_non_negative", "total_net >= 0 and total_tax >= 0 and total_gross >= 0");
                    table.ForeignKey(
                        name: "FK_quotes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotes_quote_requests_quote_request_id",
                        column: x => x.quote_request_id,
                        principalTable: "quote_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotes_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotes_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_request_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    price_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    line_net = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_items", x => x.id);
                    table.CheckConstraint("ck_quote_items_entered_positive", "entered_quantity > 0");
                    table.CheckConstraint("ck_quote_items_base_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_quote_items_unit_price_non_negative", "unit_price >= 0");
                    table.CheckConstraint("ck_quote_items_line_net_non_negative", "line_net >= 0");
                    table.ForeignKey(
                        name: "FK_quote_items_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_items_quote_request_items_quote_request_item_id",
                        column: x => x.quote_request_item_id,
                        principalTable: "quote_request_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_items_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quotes_created_by",
                table: "quotes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_customer_id",
                table: "quotes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_issued_by",
                table: "quotes",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_quote_number",
                table: "quotes",
                column: "quote_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotes_quote_request_id",
                table: "quotes",
                column: "quote_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotes_status_created_at",
                table: "quotes",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_entered_packaging_id",
                table: "quote_items",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_product_id",
                table: "quote_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_quote_id_quote_request_item_id",
                table: "quote_items",
                columns: new[] { "quote_id", "quote_request_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_quote_request_item_id",
                table: "quote_items",
                column: "quote_request_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "quote_items");
            migrationBuilder.DropTable(name: "quotes");
        }
    }
}
