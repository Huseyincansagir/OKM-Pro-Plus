# Factory ERP — PostgreSQL 18 Migration SQL Specification

**Aşama:** ARCHITECTURE

**Durum:** Ayrıntılı SQL şema tasarımı; production migration dosyası değildir.

**Baseline:** O-001–O-014 kararları 2026-08-16 tarihinde kabul edilmiştir. O-004 BOM/hammadde ve O-005 lot/seri tabloları MVP migration’ına dahil değildir.

**Amaç:** `architecture-efcore-and-migration-plan.md` içindeki 0001–0018 sırasını gerçek migration üretimine temel olacak tablo, ilişki, constraint, index, seed ve rollback sınırlarına dönüştürmek.

## 1. Ortak SQL kuralları

Aşağıdaki varsayımlar bütün migration’larda geçerlidir:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

-- Uygulama migration runner tarafından seçilir.
SET TIME ZONE 'UTC';
```

| Kural | SQL karşılığı |
|---|---|
| Kimlik | `uuid NOT NULL DEFAULT gen_random_uuid()` |
| Zaman | `timestamptz NOT NULL DEFAULT now()` |
| Miktar | `numeric(18,6)` |
| Para | `numeric(18,2)` |
| Snapshot | `jsonb NOT NULL` veya anlamlıysa nullable `jsonb` |
| Aktiflik | Master data’da `is_active`; soft delete gerekiyorsa `is_deleted` |
| Belge/ledger | Fiziksel delete yok; `status`, reversal veya credit kullanılır |
| FK delete | Varsayılan `ON DELETE RESTRICT`; yalnızca draft child için kontrollü cascade |
| Concurrency | `row_version bigint NOT NULL DEFAULT 1` |
| Audit | Transactional aggregate’lerde created/updated alanları; kritik geçişte `audit_logs` |

Her migration `Up` ve güvenli `Down` planı ile ayrı dosya olur. Ledger veya belge satırlarını silen destructive `Down` production’da otomatik çalıştırılmaz; forward-fix veya backup restore tercih edilir.

## 2. 0001 — Identity and Audit

### 2.1 Tablolar

```sql
CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username citext NOT NULL UNIQUE,
    email citext NOT NULL UNIQUE,
    password_hash text NOT NULL,
    display_name text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    last_login_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    description text,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE permissions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(120) NOT NULL UNIQUE,
    module varchar(80) NOT NULL,
    action varchar(80) NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE user_roles (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id uuid NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE role_permissions (
    role_id uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id uuid NOT NULL REFERENCES permissions(id) ON DELETE RESTRICT,
    effect varchar(20) NOT NULL DEFAULT 'Allow',
    PRIMARY KEY (role_id, permission_id),
    CHECK (effect IN ('Allow', 'Deny'))
);

CREATE TABLE user_permission_overrides (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    permission_id uuid NOT NULL REFERENCES permissions(id) ON DELETE RESTRICT,
    effect varchar(20) NOT NULL,
    reason text NOT NULL,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, permission_id),
    CHECK (effect IN ('Allow', 'Deny'))
);

CREATE TABLE refresh_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash text NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    device_info jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid REFERENCES users(id) ON DELETE RESTRICT,
    action varchar(120) NOT NULL,
    entity_type varchar(120) NOT NULL,
    entity_id uuid,
    old_values jsonb,
    new_values jsonb,
    reason text,
    ip_address inet,
    correlation_id varchar(120),
    created_at timestamptz NOT NULL DEFAULT now()
);
```

### 2.2 Index ve kabul

```sql
CREATE INDEX ix_audit_entity_time
    ON audit_logs(entity_type, entity_id, created_at DESC);
CREATE INDEX ix_refresh_user_active
    ON refresh_tokens(user_id, expires_at)
    WHERE revoked_at IS NULL;
```

Kabul: duplicate username/email reddedilir, refresh token hash dışında plaintext saklanmaz, audit insert permission/transition sırasında çalışır. Down yalnızca boş database’de identity tablolarını kaldırır.

## 3. 0002 — System Settings, Sequences and Idempotency

```sql
CREATE TABLE system_settings (
    key varchar(120) PRIMARY KEY,
    value jsonb NOT NULL,
    is_secret boolean NOT NULL DEFAULT false,
    updated_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE document_sequences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    document_type varchar(60) NOT NULL,
    year smallint NOT NULL,
    prefix varchar(20) NOT NULL,
    last_value bigint NOT NULL DEFAULT 0,
    UNIQUE (document_type, year),
    CHECK (last_value >= 0)
);

CREATE TABLE idempotency_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_scope varchar(80) NOT NULL DEFAULT 'default',
    endpoint varchar(240) NOT NULL,
    idempotency_key varchar(160) NOT NULL,
    payload_hash varchar(128) NOT NULL,
    response_status integer,
    response_body jsonb,
    resource_type varchar(120),
    resource_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz,
    UNIQUE (company_scope, endpoint, idempotency_key)
);
```

Kabul: belge numarası aynı belge tipi/yıl içinde tekrar etmez; aynı idempotency key aynı payload ile ilk response’u döndürür, farklı hash `IDEMPOTENCY_PAYLOAD_MISMATCH` üretir. Down idempotency kayıtlarını silmeden önce retention/backup kontrolü gerekir.

## 4. 0003 — UOM, Product and Packaging

```sql
CREATE TABLE units_of_measure (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(40) NOT NULL UNIQUE,
    name varchar(80) NOT NULL,
    scale smallint NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    CHECK (scale BETWEEN 0 AND 6)
);

CREATE TABLE product_categories (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    is_deleted boolean NOT NULL DEFAULT false
);

CREATE TABLE products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id uuid REFERENCES product_categories(id) ON DELETE RESTRICT,
    code varchar(80) NOT NULL UNIQUE,
    name varchar(240) NOT NULL,
    base_uom_id uuid NOT NULL REFERENCES units_of_measure(id) ON DELETE RESTRICT,
    minimum_stock_qty numeric(18,6) NOT NULL DEFAULT 0,
    is_public boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (minimum_stock_qty >= 0)
);

CREATE TABLE product_packagings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    level varchar(30) NOT NULL,
    name varchar(120) NOT NULL,
    parent_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    units_per_parent numeric(18,6),
    quantity_in_base_uom numeric(18,6) NOT NULL,
    is_sellable boolean NOT NULL DEFAULT false,
    allow_partial boolean NOT NULL DEFAULT false,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (product_id, name, effective_from),
    CHECK (level IN ('BaseUnit', 'Package', 'Case', 'Pallet')),
    CHECK (quantity_in_base_uom > 0),
    CHECK (units_per_parent IS NULL OR units_per_parent > 0),
    CHECK (effective_to IS NULL OR effective_to > effective_from)
);

CREATE TABLE product_barcodes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    barcode varchar(160) NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (barcode)
);

CREATE TABLE product_images (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    file_id uuid,
    alt_text varchar(240),
    sort_order integer NOT NULL DEFAULT 0,
    is_public boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE product_physical_profiles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    length numeric(18,6), width numeric(18,6), height numeric(18,6),
    dimension_uom varchar(20),
    net_weight numeric(18,6), weight_uom varchar(20),
    volume numeric(18,6), volume_uom varchar(20),
    is_stackable boolean NOT NULL DEFAULT false,
    max_stack_count integer,
    max_load_kg numeric(18,6),
    effective_from timestamptz NOT NULL,
    effective_to timestamptz,
    CHECK (length IS NULL OR length > 0),
    CHECK (width IS NULL OR width > 0),
    CHECK (height IS NULL OR height > 0),
    CHECK (net_weight IS NULL OR net_weight >= 0),
    CHECK (max_stack_count IS NULL OR max_stack_count > 0)
);

CREATE TABLE packaging_physical_profiles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    packaging_id uuid NOT NULL REFERENCES product_packagings(id) ON DELETE RESTRICT,
    length numeric(18,6), width numeric(18,6), height numeric(18,6),
    dimension_uom varchar(20),
    tare_weight numeric(18,6), weight_uom varchar(20),
    volume numeric(18,6), volume_uom varchar(20),
    is_stackable boolean NOT NULL DEFAULT false,
    max_stack_count integer,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz,
    CHECK (tare_weight IS NULL OR tare_weight >= 0)
);

CREATE TABLE pallet_types (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(120) NOT NULL,
    length numeric(18,6) NOT NULL,
    width numeric(18,6) NOT NULL,
    tare_weight numeric(18,6) NOT NULL DEFAULT 0,
    max_load_kg numeric(18,6),
    is_active boolean NOT NULL DEFAULT true,
    CHECK (length > 0 AND width > 0),
    CHECK (tare_weight >= 0)
);
```

Kabul: `quantity_in_base_uom` pozitif, etkin ambalaj tarihleri çakışmayan version’lar şeklinde tutulur; `5 Koli` için ayrı Product açılmaz. Down, ürün kullanılmışsa cascade delete yapmaz; pasifleştirme veya forward-fix kullanılır.

## 5. 0004 — Customers and Addresses

```sql
CREATE TABLE customers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_code varchar(80) NOT NULL UNIQUE,
    legal_name varchar(240) NOT NULL,
    tax_number varchar(40),
    tax_office varchar(160),
    email citext,
    phone varchar(40),
    status varchar(30) NOT NULL DEFAULT 'Active',
    is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status IN ('Candidate', 'Active', 'Inactive', 'Blocked'))
);

CREATE TABLE customer_addresses (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    address_type varchar(30) NOT NULL,
    title varchar(120),
    line1 varchar(240) NOT NULL,
    line2 varchar(240),
    district varchar(120),
    city varchar(120) NOT NULL,
    postal_code varchar(20),
    country_code char(2) NOT NULL DEFAULT 'TR',
    latitude numeric(10,7), longitude numeric(10,7),
    is_default boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    CHECK (address_type IN ('Billing', 'Delivery', 'Other'))
);

CREATE TABLE customer_contacts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    full_name varchar(160) NOT NULL,
    email citext,
    phone varchar(40),
    role_title varchar(120),
    is_primary boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE customer_notes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    note text NOT NULL,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now()
);
```

Kabul: public quote request customer’ı doğrudan `Active` yapamaz; duplicate eşleşmesi application command ile incelenir. Down customer verisini silmez.

## 6. 0005 — Pricing and Quote Requests

```sql
CREATE TABLE price_lists (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    valid_from timestamptz NOT NULL,
    valid_to timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE TABLE customer_price_groups (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    price_list_id uuid NOT NULL REFERENCES price_lists(id) ON DELETE RESTRICT,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE customer_price_group_members (
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    customer_price_group_id uuid NOT NULL REFERENCES customer_price_groups(id) ON DELETE RESTRICT,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz,
    PRIMARY KEY (customer_id, customer_price_group_id, effective_from),
    CHECK (effective_to IS NULL OR effective_to > effective_from)
);

CREATE TABLE product_prices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    price_list_id uuid NOT NULL REFERENCES price_lists(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    unit_price numeric(18,2) NOT NULL,
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    tax_code varchar(40),
    valid_from timestamptz NOT NULL,
    valid_to timestamptz,
    UNIQUE (price_list_id, product_id, packaging_id, valid_from),
    CHECK (unit_price >= 0),
    CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE TABLE quote_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_number varchar(80) NOT NULL UNIQUE,
    source varchar(30) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Received',
    customer_id uuid REFERENCES customers(id) ON DELETE RESTRICT,
    customer_candidate_name varchar(240),
    customer_candidate_email citext,
    customer_candidate_phone varchar(40),
    consent_at timestamptz,
    reviewed_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    reviewed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (source IN ('Public', 'Internal')),
    CHECK (status IN ('Received', 'InReview', 'Converted', 'Rejected', 'Closed'))
);

CREATE TABLE quote_request_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_request_id uuid NOT NULL REFERENCES quote_requests(id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    entered_quantity numeric(18,6) NOT NULL,
    entered_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    packaging_snapshot jsonb NOT NULL,
    CHECK (entered_quantity > 0),
    CHECK (quantity_base > 0)
);
```

Kabul: public request minimum veri ve consent ile alınır, fiyat public response’a dahil edilmez. Down price/quote history silmez; migration geri alınacaksa yalnızca boş ortamda yapılır.

## 7. 0006 — Warehouse, Stock Ledger and Reservations

```sql
CREATE TABLE warehouses (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    address_id uuid REFERENCES customer_addresses(id) ON DELETE RESTRICT,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE warehouse_locations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    code varchar(80) NOT NULL,
    name varchar(160),
    location_type varchar(30) NOT NULL DEFAULT 'Storage',
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (warehouse_id, code)
);

CREATE TABLE stocks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    location_id uuid NOT NULL REFERENCES warehouse_locations(id) ON DELETE RESTRICT,
    on_hand_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    reserved_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    available_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    row_version bigint NOT NULL DEFAULT 1,
    UNIQUE (product_id, warehouse_id, location_id),
    CHECK (on_hand_qty_base >= 0),
    CHECK (reserved_qty_base >= 0),
    CHECK (available_qty_base >= 0),
    CHECK (reserved_qty_base <= on_hand_qty_base)
);

CREATE TABLE stock_movements (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    location_id uuid NOT NULL REFERENCES warehouse_locations(id) ON DELETE RESTRICT,
    movement_type varchar(40) NOT NULL,
    quantity_base numeric(18,6) NOT NULL,
    source_entity_type varchar(120) NOT NULL,
    source_entity_id uuid NOT NULL,
    reversed_from_id uuid REFERENCES stock_movements(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (quantity_base > 0)
);

CREATE TABLE stock_reservations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_item_id uuid,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    warehouse_id uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    consumed_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    released_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    status varchar(30) NOT NULL DEFAULT 'Open',
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (quantity_base > 0),
    CHECK (consumed_qty_base >= 0 AND released_qty_base >= 0),
    CHECK (consumed_qty_base + released_qty_base <= quantity_base),
    CHECK (status IN ('Open', 'PartiallyConsumed', 'Consumed', 'Released'))
);

CREATE TABLE quantity_operation_snapshots (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_type varchar(50) NOT NULL,
    operation_id uuid NOT NULL,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    barcode_id uuid,
    operation_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    view_mode_at_entry varchar(30),
    entered_quantity numeric(18,6) NOT NULL,
    quantity_base numeric(18,6) NOT NULL,
    base_uom_id uuid NOT NULL REFERENCES units_of_measure(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    packaging_breakdown jsonb,
    warehouse_id uuid REFERENCES warehouses(id) ON DELETE RESTRICT,
    route_stop_id uuid,
    load_unit_id uuid,
    client_request_id varchar(160) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (operation_type, operation_id, client_request_id),
    CHECK (entered_quantity > 0),
    CHECK (quantity_base > 0)
);
```

Kabul: `available_qty_base = on_hand_qty_base - reserved_qty_base`; stock movement append-only, delivery issue aynı idempotency key ile ikinci çıkışı üretemez. Down stock ledger üzerinde silme yapmaz.

## 8. 0007 — Sales Orders and Approvals

```sql
CREATE TABLE sales_orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number varchar(80) NOT NULL UNIQUE,
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    status varchar(40) NOT NULL DEFAULT 'Draft',
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    price_snapshot_version varchar(120),
    total_net numeric(18,2) NOT NULL DEFAULT 0,
    total_tax numeric(18,2) NOT NULL DEFAULT 0,
    total_gross numeric(18,2) NOT NULL DEFAULT 0,
    row_version bigint NOT NULL DEFAULT 1,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status IN ('Draft', 'PendingApproval', 'Approved', 'Preparing', 'PartiallyShipped', 'Fulfilled', 'Completed', 'Cancelled'))
);

CREATE TABLE sales_order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_id uuid NOT NULL REFERENCES sales_orders(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    ordered_qty numeric(18,6) NOT NULL,
    reserved_qty numeric(18,6) NOT NULL DEFAULT 0,
    shipped_qty numeric(18,6) NOT NULL DEFAULT 0,
    cancelled_qty numeric(18,6) NOT NULL DEFAULT 0,
    remaining_qty numeric(18,6) NOT NULL DEFAULT 0,
    entered_quantity numeric(18,6) NOT NULL,
    entered_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    partial_delivery_allowed boolean NOT NULL DEFAULT true,
    unit_price numeric(18,2) NOT NULL DEFAULT 0,
    tax_code varchar(40),
    price_snapshot jsonb NOT NULL,
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (ordered_qty > 0),
    CHECK (reserved_qty >= 0 AND shipped_qty >= 0 AND cancelled_qty >= 0),
    CHECK (shipped_qty + cancelled_qty <= ordered_qty),
    CHECK (remaining_qty = ordered_qty - shipped_qty - cancelled_qty)
);

CREATE TABLE sales_order_approvals (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_id uuid NOT NULL REFERENCES sales_orders(id) ON DELETE RESTRICT,
    decision varchar(30) NOT NULL,
    comment text,
    decided_by uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    decided_at timestamptz NOT NULL DEFAULT now(),
    CHECK (decision IN ('Approved', 'Rejected'))
);
```

Kabul: approval olmadan reservation/irsaliye issue yapılamaz; `remaining_qty` stored projection olduğundan command handler ve trigger ile tutarlı tutulur.

## 9. 0008 — Delivery Notes and Shipment Allocations

```sql
CREATE TABLE delivery_notes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    document_number varchar(80) NOT NULL UNIQUE,
    sales_order_id uuid NOT NULL REFERENCES sales_orders(id) ON DELETE RESTRICT,
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Draft',
    issued_at timestamptz,
    issued_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status IN ('Draft', 'Prepared', 'ReadyToIssue', 'Issued', 'Reversed', 'Closed'))
);

CREATE TABLE delivery_note_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_note_id uuid NOT NULL REFERENCES delivery_notes(id) ON DELETE RESTRICT,
    sales_order_item_id uuid NOT NULL REFERENCES sales_order_items(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    entered_quantity numeric(18,6) NOT NULL,
    entered_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    shipped_qty numeric(18,6) NOT NULL DEFAULT 0,
    invoiced_qty numeric(18,6) NOT NULL DEFAULT 0,
    waived_qty numeric(18,6) NOT NULL DEFAULT 0,
    remaining_to_invoice numeric(18,6) NOT NULL DEFAULT 0,
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (quantity_base > 0),
    CHECK (shipped_qty >= 0 AND invoiced_qty >= 0 AND waived_qty >= 0),
    CHECK (invoiced_qty + waived_qty <= shipped_qty),
    CHECK (remaining_to_invoice = shipped_qty - invoiced_qty - waived_qty)
);

CREATE TABLE delivery_note_item_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_item_id uuid NOT NULL REFERENCES sales_order_items(id) ON DELETE RESTRICT,
    delivery_note_item_id uuid NOT NULL REFERENCES delivery_note_items(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    base_uom_id uuid NOT NULL REFERENCES units_of_measure(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Active',
    idempotency_key varchar(160) NOT NULL,
    payload_hash varchar(128) NOT NULL,
    reversed_from_id uuid REFERENCES delivery_note_item_allocations(id) ON DELETE RESTRICT,
    reversal_reason text,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (quantity_base > 0),
    CHECK (status IN ('Active', 'Reversed', 'Voided'))
);
```

Kabul: `delivery_note_item_allocations` aktif toplamı `SalesOrderItem.ordered_qty - cancelled_qty` değerini aşamaz. `DeliveryNote.Issued` olmadan invoice source oluşmaz. Down belge/allocation silmez; yalnızca boş database’de uygulanabilir.

## 10. 0009 — Vehicles, Routes and Load Plans

```sql
CREATE TABLE vehicle_types (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    inner_length numeric(18,6), inner_width numeric(18,6), inner_height numeric(18,6),
    door_width numeric(18,6), door_height numeric(18,6),
    max_gross_weight numeric(18,6), max_volume numeric(18,6),
    max_pallet_count integer,
    allowed_pallet_types jsonb,
    unloading_sides jsonb,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE vehicle_capacities (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    vehicle_type_id uuid NOT NULL REFERENCES vehicle_types(id) ON DELETE RESTRICT,
    effective_from timestamptz NOT NULL,
    effective_to timestamptz,
    max_gross_weight numeric(18,6),
    max_net_weight numeric(18,6),
    max_volume numeric(18,6),
    max_pallet_count integer,
    max_height numeric(18,6),
    capacity_policy_snapshot jsonb NOT NULL,
    UNIQUE (vehicle_type_id, effective_from),
    CHECK (effective_to IS NULL OR effective_to > effective_from)
);

CREATE TABLE vehicles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    vehicle_type_id uuid NOT NULL REFERENCES vehicle_types(id) ON DELETE RESTRICT,
    plate_number varchar(30) NOT NULL UNIQUE,
    status varchar(30) NOT NULL DEFAULT 'Available',
    maintenance_until timestamptz,
    current_route_plan_id uuid,
    last_known_location_text varchar(240),
    last_status_at timestamptz,
    CHECK (status IN ('Available', 'Assigned', 'Loading', 'InTransit', 'Maintenance', 'OutOfService'))
);

CREATE TABLE drivers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid,
    full_name varchar(160) NOT NULL,
    phone varchar(40),
    license_number varchar(80),
    license_expiry date,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE shipments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    status varchar(30) NOT NULL DEFAULT 'Preparing',
    route_plan_id uuid,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status IN ('Preparing', 'Loaded', 'InTransit', 'PartiallyDelivered', 'Delivered', 'Exception', 'Returned'))
);

CREATE TABLE shipment_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id uuid NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    delivery_note_item_id uuid NOT NULL REFERENCES delivery_note_items(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    CHECK (quantity_base > 0),
    UNIQUE (shipment_id, delivery_note_item_id)
);

CREATE TABLE route_plans (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id uuid NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    vehicle_id uuid REFERENCES vehicles(id) ON DELETE RESTRICT,
    driver_id uuid REFERENCES drivers(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Draft',
    version integer NOT NULL DEFAULT 1,
    planned_start_at timestamptz,
    planned_end_at timestamptz,
    actual_start_at timestamptz,
    actual_end_at timestamptz,
    UNIQUE (shipment_id, version)
);

CREATE TABLE route_stops (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    route_plan_id uuid NOT NULL REFERENCES route_plans(id) ON DELETE RESTRICT,
    sequence_no integer NOT NULL,
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    address_id uuid NOT NULL REFERENCES customer_addresses(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Pending',
    planned_arrival_at timestamptz,
    actual_arrival_at timestamptz,
    recipient_name varchar(160),
    proof_file_id uuid,
    exception_reason text,
    UNIQUE (route_plan_id, sequence_no)
);

CREATE TABLE shipment_packages (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id uuid NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    route_stop_id uuid REFERENCES route_stops(id) ON DELETE RESTRICT,
    parent_package_id uuid REFERENCES shipment_packages(id) ON DELETE RESTRICT,
    barcode varchar(160) NOT NULL,
    package_type varchar(30) NOT NULL,
    packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Planned',
    scanned_at timestamptz,
    delivered_at timestamptz,
    UNIQUE (shipment_id, barcode),
    CHECK (quantity_base > 0)
);

CREATE TABLE load_plans (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id uuid NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    vehicle_capacity_id uuid REFERENCES vehicle_capacities(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Draft',
    version integer NOT NULL DEFAULT 1,
    algorithm_name varchar(120),
    algorithm_version varchar(80),
    feasibility_status varchar(30),
    fit_score numeric(9,4),
    total_weight numeric(18,6),
    total_volume numeric(18,6),
    pallet_count integer,
    utilization_snapshot jsonb,
    capacity_snapshot jsonb,
    input_snapshot_hash varchar(128),
    validation_summary jsonb,
    replanned_from_id uuid REFERENCES load_plans(id) ON DELETE RESTRICT,
    locked_at timestamptz,
    locked_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    UNIQUE (shipment_id, version),
    CHECK (feasibility_status IS NULL OR feasibility_status IN ('Infeasible', 'FeasibleWithWarnings', 'Feasible'))
);

CREATE TABLE vehicle_fit_evaluations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_plan_id uuid NOT NULL REFERENCES load_plans(id) ON DELETE RESTRICT,
    vehicle_id uuid NOT NULL REFERENCES vehicles(id) ON DELETE RESTRICT,
    vehicle_capacity_id uuid NOT NULL REFERENCES vehicle_capacities(id) ON DELETE RESTRICT,
    candidate_status varchar(30) NOT NULL DEFAULT 'Candidate',
    rejection_code varchar(80),
    fit_score numeric(9,4),
    weight_ratio numeric(9,6), volume_ratio numeric(9,6), pallet_ratio numeric(9,6),
    floor_area_ratio numeric(9,6), height_ratio numeric(9,6),
    door_check_status varchar(30), dimension_check_status varchar(30),
    stacking_check_status varchar(30), axle_check_status varchar(30),
    stop_access_status varchar(30), reason_text text,
    algorithm_version varchar(80) NOT NULL,
    evaluated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (load_plan_id, vehicle_id, vehicle_capacity_id)
);

CREATE TABLE load_units (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_plan_id uuid NOT NULL REFERENCES load_plans(id) ON DELETE RESTRICT,
    pallet_type_id uuid REFERENCES pallet_types(id) ON DELETE RESTRICT,
    unit_code varchar(160) NOT NULL,
    unit_type varchar(30) NOT NULL,
    is_mixed boolean NOT NULL DEFAULT false,
    length numeric(18,6), width numeric(18,6), height numeric(18,6),
    tare_weight numeric(18,6) NOT NULL DEFAULT 0,
    gross_weight numeric(18,6) NOT NULL DEFAULT 0,
    volume numeric(18,6) NOT NULL DEFAULT 0,
    stackable boolean NOT NULL DEFAULT false,
    max_stack_count integer,
    placement_zone varchar(80),
    unloading_priority integer,
    status varchar(30) NOT NULL DEFAULT 'Planned',
    UNIQUE (load_plan_id, unit_code)
);

CREATE TABLE load_unit_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_unit_id uuid NOT NULL REFERENCES load_units(id) ON DELETE RESTRICT,
    shipment_item_id uuid NOT NULL REFERENCES shipment_items(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    entered_quantity numeric(18,6),
    quantity_base numeric(18,6) NOT NULL,
    net_weight numeric(18,6),
    volume numeric(18,6),
    compatibility_snapshot jsonb,
    stack_level integer,
    orientation varchar(30),
    packaging_snapshot jsonb NOT NULL,
    CHECK (quantity_base > 0)
);

CREATE TABLE load_unit_stop_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_unit_item_id uuid NOT NULL REFERENCES load_unit_items(id) ON DELETE RESTRICT,
    route_stop_id uuid NOT NULL REFERENCES route_stops(id) ON DELETE RESTRICT,
    package_count integer,
    quantity_base numeric(18,6) NOT NULL,
    unloading_sequence integer,
    access_priority integer,
    shipment_package_id uuid REFERENCES shipment_packages(id) ON DELETE RESTRICT,
    CHECK (quantity_base > 0)
);

CREATE TABLE load_plan_validation_results (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_plan_id uuid NOT NULL REFERENCES load_plans(id) ON DELETE RESTRICT,
    severity varchar(20) NOT NULL,
    rule_code varchar(80) NOT NULL,
    entity_type varchar(120),
    entity_id uuid,
    message text NOT NULL,
    suggested_action text,
    is_resolved boolean NOT NULL DEFAULT false,
    resolved_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    resolved_at timestamptz,
    CHECK (severity IN ('HardError', 'Warning', 'Info'))
);

CREATE TABLE load_plan_manual_changes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    load_plan_id uuid NOT NULL REFERENCES load_plans(id) ON DELETE RESTRICT,
    entity_type varchar(120) NOT NULL,
    entity_id uuid,
    previous_value jsonb NOT NULL,
    new_value jsonb NOT NULL,
    reason text NOT NULL,
    changed_by uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    changed_at timestamptz NOT NULL DEFAULT now()
);
```

Kabul: `VehicleFitEvaluation` seçilmeyen adayın rejection code ve kullanım oranlarını saklar; `LoadPlan.Locked` manuel depo onayı olmadan geçilemez. Down route/package/load plan verilerini silmez.

### 10.1 — L4-B1 implementation migration split

Bu bölümdeki 0009 şeması L4-B’nin tamamı için canonical bounded taslağı gösterir; implementation sırasında tek büyük migration yerine bounded slice sırası uygulanır. L4-A migration’ından sonra üretilen ilk L4-B migration yalnızca `shipment_packages` tablosunu oluşturur. Uygulanan migration adı `20260817081758_AddShipmentPackages` olup aşağıdaki L4-B1 kurallarını database seviyesinde enforce eder:

| Kural | Uygulama |
|---|---|
| Shipment ownership | `shipment_id` ve `shipment_item_id` için `ON DELETE RESTRICT` FK; command service aynı shipment zincirini transaction içinde doğrular |
| Route-stop ownership | `route_stop_id` için `ON DELETE RESTRICT` FK; command service route planın shipment ownership’ini doğrular |
| Server quantity | `package_count * quantity_base_per_package = quantity_base` CHECK constraint; client `quantity_base` source of truth değildir |
| Package state/type | `Case`, `Package`, `Pallet`, `Loose` ve `Available`, `Allocated`, `Loaded`, `Cancelled` CHECK constraint’leri |
| Active code uniqueness | `package_code` boş olmayan ve Cancelled olmayan kayıtlar için unique partial index |
| Physical data | `packaging_snapshot` ve `physical_snapshot` zorunlu `jsonb`; fiziksel snapshot L4-A effective profile as-of lookup ile server’da üretilir |
| Concurrency | `row_version bigint` concurrency token ve default `1` |

`load_plans`, `load_units`, `load_unit_items`, `load_unit_stop_allocations` ve `vehicle_fit_evaluations` bu migration’a eklenmez; sırasıyla L4-B2 ve L4-B3 gate’lerinde oluşturulacaktır. Down planı `shipment_packages` tablosunu yalnızca bağımlı kayıt yokken kaldırır; production’da belge/lojistik kayıtlarını silen destructive rollback yerine forward-fix veya backup restore tercih edilir.

### 10.2 — L4-B2 implementation migration split

L4-B2 migration adı `20260817091855_AddLoadPlanAndUnits` olarak üretilmiş ve L4-B1’den sonra uygulanmıştır. Bu migration `load_plans`, `load_units`, `load_unit_items` ve `load_unit_stop_allocations` tablolarını açar. Vehicle-fit evaluation, validation result, manual change, suggest, lock ve replan tabloları bu bounded slice’a eklenmez.

| Kural | Database uygulaması |
|---|---|
| Plan version uniqueness | `ux_load_plans_shipment_version` ile `(shipment_id, version)` unique |
| Plan state | Draft/Proposed/Validating/Valid/NeedsReview/Locked/Superseded CHECK |
| Feasibility | Infeasible/FeasibleWithWarnings/Feasible CHECK |
| Approval/lock pairs | `approved_by`/`approved_at` ve `locked_by`/`locked_at` çiftleri birlikte dolu veya birlikte null |
| Locked prerequisites | Locked plan için vehicle, effective capacity, input snapshot hash ve locked actor zorunlu |
| LoadUnit physical boundary | Ölçüler, hacim, dara/brüt ağırlık ve unloading priority CHECK |
| Deterministic unit order | `(load_plan_id, unloading_priority, unit_code)` index’i ve `(load_plan_id, unit_code)` unique |
| Allocation ceiling | Pozitif `quantity_base`; server command shipment item ceiling ve package ownership’i transaction içinde doğrular |
| Atomic package MVP | `ux_active_package_load_unit` aktif package’ın ikinci LoadUnit’e atanmasını engeller |
| Stop allocation | Pozitif quantity/sequence ve `(load_unit_item_id, route_stop_id)` unique |
| Concurrency | Plan, unit ve item kayıtlarında `row_version bigint` concurrency mapping’i |

L4-B2 `POST /api/v1/shipments/{shipmentId}/load-plans` yalnızca Draft üretir; araç rezervasyonu, stok hareketi, vehicle status veya FFD suggestion side-effect’i yoktur. Nested LoadUnit ve stop allocation kayıtları yalnızca aynı shipment’ın package/item/route-stop ownership zinciri doğrulandıktan sonra kaydedilir. Production Down işlemi belge ve lojistik kayıtlarını destructive biçimde silmemeli; yalnızca boş/izole database’de değerlendirilmeli veya forward-fix/backup restore kullanılmalıdır.

### 10.3 — L4-B3 implementation migration split

L4-B3 migration adı `20260817094755_AddVehicleFitEvaluations` olarak üretilmiş ve L4-B2’den sonra uygulanmıştır. Bu bounded migration yalnızca `vehicle_fit_evaluations` tablosunu oluşturur. `load_plan_validation_results`, `load_plan_manual_changes`, suggest, manual approval, lock ve replan davranışları sonraki L4-B4 gate’inde kalır.

| Kural | Database uygulaması |
|---|---|
| Candidate state | `Candidate`, `Recommended`, `Rejected`, `NeedsReview` CHECK constraint’i |
| Hard-check state | Door, dimension, stacking, axle ve stop-access alanlarında `NotChecked`, `Pass`, `Fail`, `Warning` CHECK constraint’i |
| Ratio safety | Weight, volume, pallet, floor-area, height ve fit-score oranları nullable veya `>= 0` |
| Snapshot identity | `(load_plan_id, vehicle_id, COALESCE(vehicle_capacity_id, zero_uuid), input_snapshot_hash)` unique expression index’i |
| Referential safety | LoadPlan, Vehicle ve optional VehicleCapacity FK’leri `ON DELETE RESTRICT` |
| Determinism | `algorithm_version`, `input_snapshot_hash`, `capacity_snapshot` ve evaluation timestamp saklanır; UUID/insertion order sort key değildir |
| Persistence boundary | Evaluate/candidates evaluation kaydı üretir; araç rezervasyonu, stok hareketi veya vehicle status değişikliği üretmez |

`vehicle_fit_evaluations` migration’ı local PostgreSQL baseline’ında uygulanmış ve expression unique index’i doğrudan doğrulanmıştır. FFD’nin `PlanningItem` normalization sonucu ve rejection code’ları uygulama katmanında server-side hesaplanır; client tarafından gönderilen utilization veya feasibility sonucu doğruluk kaynağı değildir. Production Down işlemi evaluation/audit geçmişini destructive biçimde silmemeli; forward-fix veya backup restore tercih edilmelidir.

## 11. 0010 — Invoices, Tax Codes and Invoice Allocations

```sql
CREATE TABLE tax_codes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(40) NOT NULL UNIQUE,
    name varchar(120) NOT NULL,
    rate numeric(9,6) NOT NULL,
    valid_from timestamptz NOT NULL,
    valid_to timestamptz,
    is_active boolean NOT NULL DEFAULT true,
    CHECK (rate >= 0 AND rate <= 1),
    CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE TABLE invoices (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_number varchar(80) NOT NULL UNIQUE,
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Draft',
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    subtotal numeric(18,2) NOT NULL DEFAULT 0,
    tax_total numeric(18,2) NOT NULL DEFAULT 0,
    grand_total numeric(18,2) NOT NULL DEFAULT 0,
    tax_snapshot jsonb NOT NULL,
    issued_at timestamptz,
    issued_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status IN ('Draft', 'ReadyToIssue', 'Issued', 'PartiallyPaid', 'Paid', 'Reversed', 'Credited')),
    CHECK (subtotal >= 0 AND tax_total >= 0 AND grand_total >= 0)
);

CREATE TABLE invoice_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id uuid NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    entered_quantity numeric(18,6) NOT NULL,
    entered_packaging_id uuid REFERENCES product_packagings(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    unit_price numeric(18,2) NOT NULL,
    tax_code_id uuid REFERENCES tax_codes(id) ON DELETE RESTRICT,
    tax_snapshot jsonb NOT NULL,
    line_total numeric(18,2) NOT NULL,
    CHECK (quantity_base > 0),
    CHECK (unit_price >= 0 AND line_total >= 0)
);

CREATE TABLE invoice_item_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_note_item_id uuid NOT NULL REFERENCES delivery_note_items(id) ON DELETE RESTRICT,
    invoice_item_id uuid NOT NULL REFERENCES invoice_items(id) ON DELETE RESTRICT,
    quantity_base numeric(18,6) NOT NULL,
    base_uom_id uuid NOT NULL REFERENCES units_of_measure(id) ON DELETE RESTRICT,
    packaging_snapshot jsonb NOT NULL,
    price_snapshot jsonb NOT NULL,
    tax_snapshot jsonb NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Active',
    idempotency_key varchar(160) NOT NULL,
    payload_hash varchar(128) NOT NULL,
    credited_from_id uuid REFERENCES invoice_item_allocations(id) ON DELETE RESTRICT,
    credit_reason text,
    created_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (quantity_base > 0),
    CHECK (status IN ('Active', 'Reversed', 'Voided'))
);
```

Kabul: source delivery note `Issued` olmadan invoice issue olmaz; aktif invoice allocation toplamı shipped/credited sınırını aşamaz; invoice issue stock movement üretmez. Down invoice/ledger silmez.

## 12. 0011 — Current Accounts, Payments and Risk

```sql
CREATE TABLE current_accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL UNIQUE REFERENCES customers(id) ON DELETE RESTRICT,
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    debit_total numeric(18,2) NOT NULL DEFAULT 0,
    credit_total numeric(18,2) NOT NULL DEFAULT 0,
    balance numeric(18,2) NOT NULL DEFAULT 0,
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (debit_total >= 0 AND credit_total >= 0)
);

CREATE TABLE current_transactions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    current_account_id uuid NOT NULL REFERENCES current_accounts(id) ON DELETE RESTRICT,
    transaction_type varchar(40) NOT NULL,
    debit_amount numeric(18,2) NOT NULL DEFAULT 0,
    credit_amount numeric(18,2) NOT NULL DEFAULT 0,
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    source_entity_type varchar(120) NOT NULL,
    source_entity_id uuid NOT NULL,
    idempotency_key varchar(160),
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (debit_amount >= 0 AND credit_amount >= 0),
    CHECK ((debit_amount > 0 AND credit_amount = 0) OR (credit_amount > 0 AND debit_amount = 0))
);

CREATE TABLE payment_methods (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(40) NOT NULL UNIQUE,
    name varchar(100) NOT NULL,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    amount numeric(18,2) NOT NULL,
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    payment_method_id uuid NOT NULL REFERENCES payment_methods(id) ON DELETE RESTRICT,
    status varchar(20) NOT NULL DEFAULT 'Draft',
    reference varchar(160),
    applied_at timestamptz,
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (amount > 0),
    CHECK (status IN ('Draft', 'Applied', 'Reversed'))
);

CREATE TABLE payment_allocations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id uuid NOT NULL REFERENCES payments(id) ON DELETE RESTRICT,
    invoice_id uuid NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
    amount numeric(18,2) NOT NULL,
    CHECK (amount > 0)
);

CREATE TABLE risk_profiles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL UNIQUE REFERENCES customers(id) ON DELETE RESTRICT,
    score numeric(9,4) NOT NULL DEFAULT 0,
    level varchar(30) NOT NULL DEFAULT 'Normal',
    soft_block boolean NOT NULL DEFAULT false,
    hard_block boolean NOT NULL DEFAULT false,
    calculated_at timestamptz,
    CHECK (score >= 0),
    CHECK (level IN ('Normal', 'Watch', 'Risky', 'Blocked'))
);

CREATE TABLE risk_calculation_runs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    algorithm_version varchar(80) NOT NULL,
    input_snapshot jsonb NOT NULL,
    score numeric(9,4) NOT NULL,
    result_snapshot jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
```

Kabul: invoice issue debit, payment apply credit, reversal/credit ayrı ledger transaction üretir. Current transaction fiziksel silinmez. O-007 soft/hard risk sonucu order approval’da snapshot olarak kullanılabilir.

## 13. 0012 — Production and Machines

O-004 gereği `production_materials` oluşturulmaz. MVP yalnızca finished-good üretim gerçekleşmesi ve stok girişi tutar.

```sql
CREATE TABLE machines (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Available',
    is_active boolean NOT NULL DEFAULT true,
    CHECK (status IN ('Available', 'Running', 'Maintenance', 'OutOfService'))
);

CREATE TABLE production_orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number varchar(80) NOT NULL UNIQUE,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    planned_qty_base numeric(18,6) NOT NULL,
    completed_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    status varchar(30) NOT NULL DEFAULT 'Planned',
    machine_id uuid REFERENCES machines(id) ON DELETE RESTRICT,
    row_version bigint NOT NULL DEFAULT 1,
    CHECK (planned_qty_base > 0),
    CHECK (completed_qty_base >= 0 AND completed_qty_base <= planned_qty_base),
    CHECK (status IN ('Planned', 'Released', 'InProgress', 'Paused', 'Completed', 'Cancelled'))
);

CREATE TABLE production_order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id uuid NOT NULL REFERENCES production_orders(id) ON DELETE RESTRICT,
    product_id uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    planned_qty_base numeric(18,6) NOT NULL,
    completed_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    CHECK (planned_qty_base > 0),
    CHECK (completed_qty_base >= 0 AND completed_qty_base <= planned_qty_base)
);

CREATE TABLE production_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id uuid NOT NULL REFERENCES production_orders(id) ON DELETE RESTRICT,
    machine_id uuid NOT NULL REFERENCES machines(id) ON DELETE RESTRICT,
    started_at timestamptz NOT NULL,
    ended_at timestamptz,
    good_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    scrap_qty_base numeric(18,6) NOT NULL DEFAULT 0,
    downtime_minutes integer NOT NULL DEFAULT 0,
    status varchar(30) NOT NULL DEFAULT 'Open',
    completed_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    CHECK (good_qty_base >= 0 AND scrap_qty_base >= 0),
    CHECK (downtime_minutes >= 0),
    CHECK (ended_at IS NULL OR ended_at >= started_at),
    CHECK (status IN ('Open', 'Completed', 'Cancelled'))
);

CREATE TABLE production_personnel (
    production_record_id uuid NOT NULL REFERENCES production_records(id) ON DELETE RESTRICT,
    employee_id uuid NOT NULL,
    worked_minutes integer NOT NULL,
    PRIMARY KEY (production_record_id, employee_id),
    CHECK (worked_minutes >= 0)
);

CREATE TABLE machine_downtimes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    machine_id uuid NOT NULL REFERENCES machines(id) ON DELETE RESTRICT,
    production_record_id uuid REFERENCES production_records(id) ON DELETE RESTRICT,
    reason_code varchar(80) NOT NULL,
    started_at timestamptz NOT NULL,
    ended_at timestamptz,
    minutes integer,
    CHECK (ended_at IS NULL OR ended_at >= started_at),
    CHECK (minutes IS NULL OR minutes >= 0)
);

CREATE TABLE production_quality_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    production_record_id uuid NOT NULL REFERENCES production_records(id) ON DELETE RESTRICT,
    result varchar(30) NOT NULL,
    note text,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (result IN ('Accepted', 'Rejected', 'Review'))
);
```

Kabul: production complete `StockMovement(ProductionReceipt)` üretir; hammadde çıkışı yoktur. Down production ledger’ını silmez.

## 14. 0013 — Employees and Attendance

```sql
CREATE TABLE departments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(80) NOT NULL UNIQUE,
    name varchar(160) NOT NULL,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE employees (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_number varchar(80) NOT NULL UNIQUE,
    full_name varchar(160) NOT NULL,
    national_id_masked varchar(80),
    email citext,
    phone varchar(40),
    hire_date date,
    status varchar(30) NOT NULL DEFAULT 'Active',
    CHECK (status IN ('Active', 'Inactive', 'OnLeave'))
);

CREATE TABLE employee_departments (
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    department_id uuid NOT NULL REFERENCES departments(id) ON DELETE RESTRICT,
    effective_from date NOT NULL,
    effective_to date,
    PRIMARY KEY (employee_id, department_id, effective_from),
    CHECK (effective_to IS NULL OR effective_to >= effective_from)
);

CREATE TABLE attendance_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    work_date date NOT NULL,
    check_in_at timestamptz,
    check_out_at timestamptz,
    source varchar(30) NOT NULL DEFAULT 'Manual',
    status varchar(30) NOT NULL DEFAULT 'Open',
    UNIQUE (employee_id, work_date),
    CHECK (source IN ('Manual', 'Device', 'Import'))
);

CREATE TABLE overtime_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    work_date date NOT NULL,
    minutes integer NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Pending',
    approved_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    CHECK (minutes > 0),
    CHECK (status IN ('Pending', 'Approved', 'Rejected'))
);

CREATE TABLE leave_types (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code varchar(60) NOT NULL UNIQUE,
    name varchar(120) NOT NULL,
    is_paid boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true
);

CREATE TABLE leave_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    leave_type_id uuid NOT NULL REFERENCES leave_types(id) ON DELETE RESTRICT,
    starts_on date NOT NULL,
    ends_on date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Pending',
    reason text,
    approved_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    CHECK (ends_on >= starts_on),
    CHECK (status IN ('Pending', 'Approved', 'Rejected', 'Cancelled'))
);

CREATE TABLE salary_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    period_year smallint NOT NULL,
    period_month smallint NOT NULL,
    gross_amount numeric(18,2),
    net_amount numeric(18,2),
    currency_code char(3) NOT NULL DEFAULT 'TRY',
    is_final boolean NOT NULL DEFAULT false,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    UNIQUE (employee_id, period_year, period_month),
    CHECK (period_month BETWEEN 1 AND 12),
    CHECK (gross_amount IS NULL OR gross_amount >= 0),
    CHECK (net_amount IS NULL OR net_amount >= 0)
);

CREATE TABLE employee_documents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE RESTRICT,
    file_id uuid,
    document_type varchar(80) NOT NULL,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now()
);
```

Kabul: maaş response ve export’ları permission/field masking/audit ile korunur; bordro motoru veya resmi beyan entegrasyonu yoktur. Down maaş/attendance history silmez.

## 15. 0014 — Notifications and Files

```sql
CREATE TABLE files (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    storage_key varchar(320) NOT NULL UNIQUE,
    original_name varchar(240) NOT NULL,
    content_type varchar(160) NOT NULL,
    size_bytes bigint NOT NULL,
    sha256 varchar(64),
    is_private boolean NOT NULL DEFAULT true,
    created_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (size_bytes >= 0)
);

CREATE TABLE notifications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    type varchar(80) NOT NULL,
    title varchar(240) NOT NULL,
    body text NOT NULL,
    entity_type varchar(120),
    entity_id uuid,
    severity varchar(20) NOT NULL DEFAULT 'Info',
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE notification_recipients (
    notification_id uuid NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_read boolean NOT NULL DEFAULT false,
    read_at timestamptz,
    PRIMARY KEY (notification_id, user_id)
);
```

Kabul: file metadata database’de, binary dosya private volume veya S3-compatible storage’da tutulur; notification duplicate key application job ile kontrol edilir.

## 16. 0015 — Indexes and Constraints

Bu migration tablo kurmaz; önceki tablolara performans ve business invariant index’leri ekler.

```sql
CREATE INDEX ix_products_public_active
    ON products(is_public, is_active)
    WHERE is_deleted = false;

CREATE UNIQUE INDEX ux_active_barcode
    ON product_barcodes(barcode)
    WHERE is_active = true;

CREATE INDEX ix_packaging_product_effective
    ON product_packagings(product_id, level, effective_from DESC);

CREATE INDEX ix_stock_product_warehouse
    ON stocks(product_id, warehouse_id, location_id);

CREATE INDEX ix_stock_movements_product_time
    ON stock_movements(product_id, warehouse_id, created_at DESC);

CREATE INDEX ix_orders_customer_status_time
    ON sales_orders(customer_id, status, created_at DESC);

CREATE INDEX ix_invoices_customer_status_time
    ON invoices(customer_id, status, issued_at DESC);

CREATE INDEX ix_delivery_allocation_source
    ON delivery_note_item_allocations(sales_order_item_id, status);

CREATE UNIQUE INDEX ux_active_delivery_source_target
    ON delivery_note_item_allocations(sales_order_item_id, delivery_note_item_id)
    WHERE status = 'Active';

CREATE INDEX ix_invoice_allocation_source
    ON invoice_item_allocations(delivery_note_item_id, status);

CREATE UNIQUE INDEX ux_active_invoice_source_target
    ON invoice_item_allocations(delivery_note_item_id, invoice_item_id)
    WHERE status = 'Active';

CREATE INDEX ix_vehicle_candidates_fit
    ON vehicle_fit_evaluations(load_plan_id, candidate_status, fit_score DESC);

CREATE INDEX ix_route_stop_status_time
    ON route_stops(status, planned_arrival_at);

CREATE INDEX ix_current_transactions_account_time
    ON current_transactions(current_account_id, created_at DESC);

CREATE INDEX ix_audit_entity_time_2
    ON audit_logs(entity_type, entity_id, created_at DESC);

CREATE INDEX ix_public_quote_status_time
    ON quote_requests(status, created_at DESC);
```

Ek `CHECK` kuralları:

```sql
ALTER TABLE stocks
  ADD CONSTRAINT ck_stocks_available_formula
  CHECK (available_qty_base = on_hand_qty_base - reserved_qty_base);

ALTER TABLE delivery_note_items
  ADD CONSTRAINT ck_delivery_item_invoice_limit
  CHECK (invoiced_qty + waived_qty <= shipped_qty);

ALTER TABLE sales_order_items
  ADD CONSTRAINT ck_order_item_remaining_formula
  CHECK (remaining_qty = ordered_qty - shipped_qty - cancelled_qty);
```

Kabul: `EXPLAIN (ANALYZE, BUFFERS)` representative list/report queries üzerinde incelenir; concurrent allocation toplamı yalnızca index ile değil 0016 trigger/application lock ile de korunur.

## 17. 0016 — Row Version and Deferred Business Constraints

### 17.1 Row-version trigger

```sql
CREATE OR REPLACE FUNCTION increment_row_version()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.row_version := OLD.row_version + 1;
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_sales_orders_row_version
BEFORE UPDATE ON sales_orders
FOR EACH ROW EXECUTE FUNCTION increment_row_version();

CREATE TRIGGER trg_sales_order_items_row_version
BEFORE UPDATE ON sales_order_items
FOR EACH ROW EXECUTE FUNCTION increment_row_version();

CREATE TRIGGER trg_delivery_notes_row_version
BEFORE UPDATE ON delivery_notes
FOR EACH ROW EXECUTE FUNCTION increment_row_version();

CREATE TRIGGER trg_delivery_note_items_row_version
BEFORE UPDATE ON delivery_note_items
FOR EACH ROW EXECUTE FUNCTION increment_row_version();

CREATE TRIGGER trg_invoices_row_version
BEFORE UPDATE ON invoices
FOR EACH ROW EXECUTE FUNCTION increment_row_version();

CREATE TRIGGER trg_stocks_row_version
BEFORE UPDATE ON stocks
FOR EACH ROW EXECUTE FUNCTION increment_row_version();
```

Gerçek migration’da trigger listesi `row_version` taşıyan tüm aggregate root/summary tabloları için generated olarak kontrol edilir.

### 17.2 Allocation upper-bound trigger

```sql
CREATE OR REPLACE FUNCTION validate_delivery_allocation_total()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    allowed_qty numeric(18,6);
    used_qty numeric(18,6);
BEGIN
    SELECT ordered_qty - cancelled_qty
      INTO allowed_qty
      FROM sales_order_items
     WHERE id = NEW.sales_order_item_id
     FOR UPDATE;

    SELECT COALESCE(SUM(quantity_base), 0)
      INTO used_qty
      FROM delivery_note_item_allocations
     WHERE sales_order_item_id = NEW.sales_order_item_id
       AND status = 'Active'
       AND id <> NEW.id;

    IF used_qty + NEW.quantity_base > allowed_qty THEN
        RAISE EXCEPTION 'delivery allocation exceeds remaining order quantity'
            USING ERRCODE = '23514',
                  DETAIL = 'QUANTITY_ALLOCATION_EXCEEDED';
    END IF;
    RETURN NEW;
END;
$$;

CREATE CONSTRAINT TRIGGER trg_validate_delivery_allocation_total
AFTER INSERT OR UPDATE OF quantity_base, status
ON delivery_note_item_allocations
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION validate_delivery_allocation_total();
```

Invoice allocation için aynı desen `DeliveryNoteItem.shipped_qty - invoiced_qty - waived_qty` limitine uygulanır. Trigger exception application layer’da `OVER_ALLOCATION` veya `QUANTITY_CONCURRENCY_CONFLICT` problem response’una map edilir.

Kabul: aynı source item üzerinde iki concurrent issue transaction’ı test edilir; biri commit, diğeri rollback/conflict üretmelidir. Destructive `Down` yalnızca trigger ve function bağımlılıkları boş ortamda kaldırır.

## 18. 0017 — Permission and Baseline Settings Seeds

Migration planında permission seed ayrı tutulur; uygulama davranışını ve default role kapsamını belirler.

### 18.1 0017 permission seed

```sql
INSERT INTO permissions(code, module, action, description)
VALUES
 ('product.read', 'Products', 'Read', 'Ürünleri görüntüle'),
 ('product.create', 'Products', 'Create', 'Ürün oluştur'),
 ('stock.read', 'Warehouse', 'Read', 'Stok görüntüle'),
 ('stock.count', 'Warehouse', 'Count', 'Sayım yap'),
 ('stock.transfer', 'Warehouse', 'Transfer', 'Depolar arası transfer'),
 ('stock.adjust', 'Warehouse', 'Adjust', 'Yetkili stok düzeltmesi'),
 ('order.read', 'Sales', 'Read', 'Sipariş görüntüle'),
 ('order.create', 'Sales', 'Create', 'Sipariş oluştur'),
 ('order.approve', 'Sales', 'Approve', 'Sipariş onayla'),
 ('delivery-note.create', 'Shipping', 'Create', 'İrsaliye taslağı oluştur'),
 ('delivery-note.issue', 'Shipping', 'Issue', 'İrsaliye kesinleştir'),
 ('delivery-note.reverse', 'Shipping', 'Reverse', 'İrsaliye ters kaydı'),
 ('invoice.create', 'Invoicing', 'Create', 'Fatura taslağı oluştur'),
 ('invoice.issue', 'Invoicing', 'Issue', 'Fatura kesinleştir'),
 ('invoice.reverse', 'Invoicing', 'Reverse', 'Fatura ters kaydı'),
 ('payment.create', 'Payments', 'Create', 'Ödeme kaydet'),
 ('shipment.vehicle-fit', 'Shipping', 'VehicleFit', 'Araç uygunluğu değerlendir'),
 ('shipment.plan-suggest', 'Shipping', 'PlanSuggest', 'Kargo planı öner'),
 ('shipment.plan-override', 'Shipping', 'PlanOverride', 'Kargo planı override'),
 ('shipment.deliver', 'Shipping', 'Deliver', 'Teslim kaydı'),
 ('salary.read', 'Employees', 'SalaryRead', 'Maaş özetini görüntüle'),
 ('salary.export', 'Employees', 'SalaryExport', 'Maaş export'),
 ('report.read', 'Reporting', 'Read', 'Rapor görüntüle'),
 ('report.export', 'Reporting', 'Export', 'Rapor export')
ON CONFLICT (code) DO UPDATE
SET module = EXCLUDED.module,
    action = EXCLUDED.action,
    description = EXCLUDED.description;
```

İlk roller `Admin`, `SalesManager`, `WarehouseManager`, `WarehouseOperator`, `Accounting`, `ProductionManager`, `HR`, `ReportViewer` olarak seed edilebilir. Secret admin password seed’e yazılmaz; ilk kullanıcı bootstrap akışıyla oluşturulur.

## 19. 0018 — Reference Data Seeds

Reference seed master data davranışını belirler; gerçek ticari veriler import/master-data işleminden gelir.

### 19.1 0018 reference seed

```sql
INSERT INTO units_of_measure(code, name, scale)
VALUES
 ('Piece', 'Adet', 0),
 ('Kilogram', 'Kilogram', 3),
 ('Meter', 'Metre', 3),
 ('Liter', 'Litre', 3)
ON CONFLICT (code) DO NOTHING;

INSERT INTO payment_methods(code, name)
VALUES
 ('Cash', 'Nakit'),
 ('BankTransfer', 'Havale/EFT'),
 ('Card', 'Kredi Kartı'),
 ('Cheque', 'Çek'),
 ('PromissoryNote', 'Senet')
ON CONFLICT (code) DO NOTHING;

INSERT INTO system_settings(key, value)
VALUES
 ('default_currency', '"TRY"'::jsonb),
 ('default_timezone', '"Europe/Istanbul"'::jsonb),
 ('backup_retention_days', '14'::jsonb),
 ('rpo_hours', '24'::jsonb),
 ('rto_hours', '8'::jsonb)
ON CONFLICT (key) DO NOTHING;
```

Tax code seed’leri örnek/configuration olarak tutulur; gerçek oran ve vergi kodları mali müşavir doğrulamasıyla import edilir. Packaging level referansları `BaseUnit`, `Package`, `Case`, `Pallet` olarak sabit kodlanır; ürün bazlı katsayılar import/master-data işleminden gelir.

## 20. Migration çalışma sırası ve test kapıları

```text
Clean PostgreSQL
→ 0001–0002 identity/system
→ 0003–0005 product/customer/pricing
→ 0006–0008 stock/order/delivery
→ 0009–0011 shipping/invoice/current
→ 0012–0014 production/personnel/files
→ 0015 indexes/checks
→ 0016 triggers/concurrency
→ 0017 permissions
→ 0018 reference seeds
→ schema snapshot/hash
→ API readiness smoke test
```

Her adım için şu acceptance kaydı tutulur: migration name, start/end timestamp, schema version, affected tables, row counts, constraint/index check, seed idempotency sonucu, rollback/forward-fix kararı ve operator.

## 21. Rollback ve forward-fix matrisi

| Migration | Güvenli Down | Production yaklaşımı |
|---|---|---|
| 0001–0002 | Boş ortamda mümkün | Backup/restore; user/audit/idempotency silme yok |
| 0003–0005 | Boş veya kullanılmamış master data | Pasifleştirme ve forward-fix |
| 0006–0008 | Destructive değil | Ledger/document silinmez; reversal |
| 0009 | Destructive değil | Plan/route version ile düzeltme |
| 0010–0011 | Destructive değil | Invoice/current/payment reversal veya credit |
| 0012–0014 | Destructive değil | History korunur; yeni migration |
| 0015–0016 | Constraint/trigger geri alınabilir | Önce violation/backfill çözülür |
| 0017–0018 | Seed update/idempotent | Kod/seed forward-fix |

Bu belge gerçek migration class veya SQL deployment script’i değildir. `factory-erp-architecture` acceptance tamamlandıktan sonra EF Core migration dosyaları bu sözleşmeyi executable hale getirebilir.
