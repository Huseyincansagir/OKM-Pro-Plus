using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesQuoteReservationsAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_price_group_members",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_price_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_price_group_members", x => new { x.customer_id, x.customer_price_group_id, x.effective_from });
                    table.CheckConstraint("ck_customer_price_group_members_valid_window", "effective_to is null or effective_to > effective_from");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    tax_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    email = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.CheckConstraint("ck_customers_status", "status in ('Candidate', 'Active', 'Inactive', 'Blocked')");
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_lists", x => x.id);
                    table.CheckConstraint("ck_price_lists_valid_window", "valid_to is null or valid_to > valid_from");
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    line1 = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    line2 = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    district = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.id);
                    table.CheckConstraint("ck_customer_addresses_type", "address_type in ('Billing', 'Delivery', 'Other')");
                    table.ForeignKey(
                        name: "FK_customer_addresses_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    email = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    role_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_contacts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_candidate_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    customer_candidate_email = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    customer_candidate_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    consent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_requests", x => x.id);
                    table.CheckConstraint("ck_quote_requests_source", "source in ('Public', 'Internal')");
                    table.CheckConstraint("ck_quote_requests_status", "status in ('Received', 'InReview', 'Converted', 'Rejected', 'Closed')");
                    table.ForeignKey(
                        name: "FK_quote_requests_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_requests_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    price_snapshot_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    total_net = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_gross = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_orders", x => x.id);
                    table.CheckConstraint("ck_sales_orders_status", "status in ('Draft', 'PendingApproval', 'Approved', 'Preparing', 'PartiallyShipped', 'Fulfilled', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_sales_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_price_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_price_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_price_groups_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_prices", x => x.id);
                    table.CheckConstraint("ck_product_prices_non_negative", "unit_price >= 0");
                    table.CheckConstraint("ck_product_prices_valid_window", "valid_to is null or valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_product_prices_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_prices_product_packagings_packaging_id",
                        column: x => x.packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_prices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_request_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_request_items", x => x.id);
                    table.CheckConstraint("ck_quote_request_items_base_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_quote_request_items_entered_positive", "entered_quantity > 0");
                    table.ForeignKey(
                        name: "FK_quote_request_items_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_request_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quote_request_items_quote_requests_quote_request_id",
                        column: x => x.quote_request_id,
                        principalTable: "quote_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_approvals", x => x.id);
                    table.CheckConstraint("ck_sales_order_approvals_decision", "decision in ('Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_sales_order_approvals_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_approvals_users_decided_by",
                        column: x => x.decided_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    reserved_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    shipped_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cancelled_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    remaining_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    entered_packaging_id = table.Column<Guid>(type: "uuid", nullable: true),
                    packaging_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    partial_delivery_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    price_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_items", x => x.id);
                    table.CheckConstraint("ck_sales_order_items_components_non_negative", "reserved_qty >= 0 and shipped_qty >= 0 and cancelled_qty >= 0");
                    table.CheckConstraint("ck_sales_order_items_ordered_positive", "ordered_qty > 0");
                    table.CheckConstraint("ck_sales_order_items_remaining_projection", "remaining_qty = ordered_qty - shipped_qty - cancelled_qty");
                    table.CheckConstraint("ck_sales_order_items_shipped_within_ordered", "shipped_qty + cancelled_qty <= ordered_qty");
                    table.ForeignKey(
                        name: "FK_sales_order_items_product_packagings_entered_packaging_id",
                        column: x => x.entered_packaging_id,
                        principalTable: "product_packagings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_items_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    consumed_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    released_qty_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    row_version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                    table.CheckConstraint("ck_stock_reservations_components_non_negative", "consumed_qty_base >= 0 and released_qty_base >= 0");
                    table.CheckConstraint("ck_stock_reservations_components_within_quantity", "consumed_qty_base + released_qty_base <= quantity_base");
                    table.CheckConstraint("ck_stock_reservations_quantity_positive", "quantity_base > 0");
                    table.CheckConstraint("ck_stock_reservations_status", "status in ('Open', 'PartiallyConsumed', 'Consumed', 'Released')");
                    table.ForeignKey(
                        name: "FK_stock_reservations_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_sales_order_items_sales_order_item_id",
                        column: x => x.sales_order_item_id,
                        principalTable: "sales_order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_id_address_type_is_default",
                table: "customer_addresses",
                columns: new[] { "customer_id", "address_type", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_contacts_customer_id_is_primary",
                table: "customer_contacts",
                columns: new[] { "customer_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_price_groups_code",
                table: "customer_price_groups",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_price_groups_price_list_id",
                table: "customer_price_groups",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_customer_code",
                table: "customers",
                column: "customer_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_status_is_deleted",
                table: "customers",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_code",
                table: "price_lists",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_packaging_id",
                table: "product_prices",
                column: "packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_price_list_id_product_id_packaging_id_valid_~",
                table: "product_prices",
                columns: new[] { "price_list_id", "product_id", "packaging_id", "valid_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_product_id",
                table: "product_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_request_items_entered_packaging_id",
                table: "quote_request_items",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_request_items_product_id",
                table: "quote_request_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_request_items_quote_request_id",
                table: "quote_request_items",
                column: "quote_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_customer_id",
                table: "quote_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_request_number",
                table: "quote_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_reviewed_by",
                table: "quote_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_status_created_at",
                table: "quote_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_approvals_decided_by",
                table: "sales_order_approvals",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_approvals_sales_order_id_decided_at",
                table: "sales_order_approvals",
                columns: new[] { "sales_order_id", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_entered_packaging_id",
                table: "sales_order_items",
                column: "entered_packaging_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_product_id",
                table: "sales_order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_sales_order_id_product_id",
                table: "sales_order_items",
                columns: new[] { "sales_order_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_created_by",
                table: "sales_orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_customer_id",
                table: "sales_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_order_number",
                table: "sales_orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_status_created_at",
                table: "sales_orders",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_product_id",
                table: "stock_reservations",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_sales_order_item_id_status",
                table: "stock_reservations",
                columns: new[] { "sales_order_item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_warehouse_id",
                table: "stock_reservations",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_addresses");

            migrationBuilder.DropTable(
                name: "customer_contacts");

            migrationBuilder.DropTable(
                name: "customer_price_group_members");

            migrationBuilder.DropTable(
                name: "customer_price_groups");

            migrationBuilder.DropTable(
                name: "product_prices");

            migrationBuilder.DropTable(
                name: "quote_request_items");

            migrationBuilder.DropTable(
                name: "sales_order_approvals");

            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropTable(
                name: "quote_requests");

            migrationBuilder.DropTable(
                name: "sales_order_items");

            migrationBuilder.DropTable(
                name: "sales_orders");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
