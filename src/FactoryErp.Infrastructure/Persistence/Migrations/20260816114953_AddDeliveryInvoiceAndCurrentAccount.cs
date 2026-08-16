using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryInvoiceAndCurrentAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "current_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    debit_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_accounts", x => x.id);
                    table.CheckConstraint("ck_current_accounts_totals_non_negative", "debit_total >= 0 and credit_total >= 0");
                    table.ForeignKey(
                        name: "FK_current_accounts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_notes", x => x.id);
                    table.CheckConstraint("ck_delivery_notes_status", "status in ('Draft', 'Prepared', 'ReadyToIssue', 'Issued', 'Reversed', 'Closed')");
                    table.ForeignKey(
                        name: "FK_delivery_notes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_notes_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_notes_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.CheckConstraint("ck_invoices_status", "status in ('Draft', 'ReadyToIssue', 'Issued', 'PartiallyPaid', 'Paid', 'Reversed', 'Credited')");
                    table.CheckConstraint("ck_invoices_totals_non_negative", "subtotal >= 0 and tax_total >= 0 and grand_total >= 0");
                    table.ForeignKey(
                        name: "FK_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_codes", x => x.id);
                    table.CheckConstraint("ck_tax_codes_rate", "rate >= 0 and rate <= 1");
                    table.CheckConstraint("ck_tax_codes_valid_window", "valid_to is null or valid_to > valid_from");
                });

            migrationBuilder.CreateTable(
                name: "current_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source_entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_transactions", x => x.id);
                    table.CheckConstraint("ck_current_transactions_amounts_non_negative", "debit_amount >= 0 and credit_amount >= 0");
                    table.CheckConstraint("ck_current_transactions_one_side", "(debit_amount > 0 and credit_amount = 0) or (credit_amount > 0 and debit_amount = 0)");
                    table.ForeignKey(
                        name: "FK_current_transactions_current_accounts_current_account_id",
                        column: x => x.current_account_id,
                        principalTable: "current_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_current_transactions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_note_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    shipped_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    invoiced_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    waived_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    remaining_to_invoice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_note_items", x => x.id);
                    table.CheckConstraint("ck_delivery_note_items_components_non_negative", "shipped_qty >= 0 and invoiced_qty >= 0 and waived_qty >= 0");
                    table.CheckConstraint("ck_delivery_note_items_invoiced_within_shipped", "invoiced_qty + waived_qty <= shipped_qty");
                    table.CheckConstraint("ck_delivery_note_items_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_delivery_note_items_remaining_projection", "remaining_to_invoice = shipped_qty - invoiced_qty - waived_qty");
                    table.ForeignKey(
                        name: "FK_delivery_note_items_delivery_notes_delivery_note_id",
                        column: x => x.delivery_note_id,
                        principalTable: "delivery_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_items_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_items_sales_order_items_sales_order_item_id",
                        column: x => x.sales_order_item_id,
                        principalTable: "sales_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_payments_status", "status in ('Draft', 'Applied', 'Reversed')");
                    table.ForeignKey(
                        name: "FK_payments_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_note_item_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reversed_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_note_item_allocations", x => x.id);
                    table.CheckConstraint("ck_delivery_note_allocations_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_delivery_note_allocations_status", "status in ('Active', 'Reversed', 'Voided')");
                    table.ForeignKey(
                        name: "FK_delivery_note_item_allocations_delivery_note_item_allocatio~",
                        column: x => x.reversed_from_id,
                        principalTable: "delivery_note_item_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_item_allocations_delivery_note_items_delivery~",
                        column: x => x.delivery_note_item_id,
                        principalTable: "delivery_note_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_item_allocations_sales_order_items_sales_orde~",
                        column: x => x.sales_order_item_id,
                        principalTable: "sales_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_item_allocations_units_of_measure_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "units_of_measure",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_note_item_allocations_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_items", x => x.id);
                    table.CheckConstraint("ck_invoice_items_amounts_non_negative", "unit_price >= 0 and line_total >= 0");
                    table.CheckConstraint("ck_invoice_items_quantity_positive", "quantity_base > 0");
                    table.ForeignKey(
                        name: "FK_invoice_items_delivery_note_items_delivery_note_item_id",
                        column: x => x.delivery_note_item_id,
                        principalTable: "delivery_note_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_items_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_items_tax_codes_tax_code_id",
                        column: x => x.tax_code_id,
                        principalTable: "tax_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_payment_allocations_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "FK_payment_allocations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_item_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    price_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    tax_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    credited_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_item_allocations", x => x.id);
                    table.CheckConstraint("ck_invoice_allocations_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_invoice_allocations_status", "status in ('Active', 'Reversed', 'Voided')");
                    table.ForeignKey(
                        name: "FK_invoice_item_allocations_delivery_note_items_delivery_note_~",
                        column: x => x.delivery_note_item_id,
                        principalTable: "delivery_note_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_item_allocations_invoice_item_allocations_credited_~",
                        column: x => x.credited_from_id,
                        principalTable: "invoice_item_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_item_allocations_invoice_items_invoice_item_id",
                        column: x => x.invoice_item_id,
                        principalTable: "invoice_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_item_allocations_units_of_measure_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "units_of_measure",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_current_accounts_customer_id",
                table: "current_accounts",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_current_transactions_created_by",
                table: "current_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_current_transactions_current_account_id",
                table: "current_transactions",
                column: "current_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_current_transactions_source_entity_type_source_entity_id_id~",
                table: "current_transactions",
                columns: new[] { "source_entity_type", "source_entity_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key is not null");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_base_uom_id",
                table: "delivery_note_item_allocations",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_created_by",
                table: "delivery_note_item_allocations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_delivery_note_item_id",
                table: "delivery_note_item_allocations",
                column: "delivery_note_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_idempotency_key",
                table: "delivery_note_item_allocations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_reversed_from_id",
                table: "delivery_note_item_allocations",
                column: "reversed_from_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_item_allocations_sales_order_item_id_status",
                table: "delivery_note_item_allocations",
                columns: new[] { "sales_order_item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_items_delivery_note_id_sales_order_item_id",
                table: "delivery_note_items",
                columns: new[] { "delivery_note_id", "sales_order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_items_entered_packaging_id",
                table: "delivery_note_items",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_items_product_id",
                table: "delivery_note_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_note_items_sales_order_item_id",
                table: "delivery_note_items",
                column: "sales_order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_notes_customer_id",
                table: "delivery_notes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_notes_document_number",
                table: "delivery_notes",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_notes_issued_by",
                table: "delivery_notes",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_notes_sales_order_id_status",
                table: "delivery_notes",
                columns: new[] { "sales_order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_allocations_base_uom_id",
                table: "invoice_item_allocations",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_allocations_credited_from_id",
                table: "invoice_item_allocations",
                column: "credited_from_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_allocations_delivery_note_item_id_status",
                table: "invoice_item_allocations",
                columns: new[] { "delivery_note_item_id", "status" });

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
                name: "IX_invoice_items_delivery_note_item_id",
                table: "invoice_items",
                column: "delivery_note_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_entered_packaging_id",
                table: "invoice_items",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_invoice_id_delivery_note_item_id",
                table: "invoice_items",
                columns: new[] { "invoice_id", "delivery_note_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_product_id",
                table: "invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_tax_code_id",
                table: "invoice_items",
                column: "tax_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_customer_id_status_created_at",
                table: "invoices",
                columns: new[] { "customer_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_issued_by",
                table: "invoices",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_invoice_id",
                table: "payment_allocations",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id_invoice_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_code",
                table: "payment_methods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_customer_id_status_applied_at",
                table: "payments",
                columns: new[] { "customer_id", "status", "applied_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_payment_method_id",
                table: "payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_codes_code",
                table: "tax_codes",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_transactions");

            migrationBuilder.DropTable(
                name: "delivery_note_item_allocations");

            migrationBuilder.DropTable(
                name: "invoice_item_allocations");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "current_accounts");

            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "delivery_note_items");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "tax_codes");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "delivery_notes");
        }
    }
}
