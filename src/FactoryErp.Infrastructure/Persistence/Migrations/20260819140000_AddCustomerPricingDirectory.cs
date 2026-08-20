using System;
using FactoryErp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FactoryErpDbContext))]
    [Migration("20260819140000_AddCustomerPricingDirectory")]
    public partial class AddCustomerPricingDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "list_unit_price",
                table: "quote_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "quote_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_price_list_id",
                table: "quote_items",
                column: "price_list_id");

            migrationBuilder.AddForeignKey(
                name: "FK_quote_items_price_lists_price_list_id",
                table: "quote_items",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "customer_outbound_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_email = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    subject = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_outbound_emails", x => x.id);
                    table.CheckConstraint("ck_customer_outbound_emails_status", "status in ('Queued', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_customer_outbound_emails_customer_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "customer_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_outbound_emails_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_outbound_emails_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_outbound_emails_contact_id",
                table: "customer_outbound_emails",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_outbound_emails_created_by",
                table: "customer_outbound_emails",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_customer_outbound_emails_customer_id_created_at",
                table: "customer_outbound_emails",
                columns: new[] { "customer_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "customer_outbound_emails");

            migrationBuilder.DropForeignKey(
                name: "FK_quote_items_price_lists_price_list_id",
                table: "quote_items");

            migrationBuilder.DropIndex(
                name: "IX_quote_items_price_list_id",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "list_unit_price",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "quote_items");
        }
    }
}
