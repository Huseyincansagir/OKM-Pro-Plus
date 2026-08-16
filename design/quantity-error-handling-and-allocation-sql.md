# Factory ERP — Quantity Error Handling and Allocation SQL Specification

**Kapsam:** `QUANTITY_BASE_MISMATCH`, `QUANTITY_CONCURRENCY_CONFLICT`, idempotency ve O-002/O-003 allocation persistence

**Durum:** Tasarım önerisi; Design Gate kapanmadan production contract değildir.

**İlgili canonical belgeler:** [`partial-shipment-invoicing-workflow.md`](./partial-shipment-invoicing-workflow.md), [`database-technical-architecture.md`](./database-technical-architecture.md), [`domain-model.md`](./domain-model.md)

## 1. Tasarım ilkeleri

Miktar, stok ve cari hareketleri yalnızca backend use-case'leri üzerinden oluşturulur. API istemcisi ekranda gördüğü koli/paket/palet karşılığını gönderebilir; ancak `quantity_base` değerinin doğruluğu server-side ambalaj katsayısı, UOM precision ve güncel allocation projection'ı ile belirlenir.

> **Temel kural:** Hata durumunda sistem kısmi başarı üretmemelidir. İrsaliye kesinleştirme başarısızsa stok hareketi, rezervasyon tüketimi ve sipariş projection'ı; fatura kesinleştirme başarısızsa invoice allocation ve cari borç hareketi birlikte rollback edilmelidir.

İki hata sınıfı birbirinden ayrılır:

| Hata | Sınıf | İstemci davranışı |
|---|---|---|
| `QUANTITY_BASE_MISMATCH` | Deterministik veri/hesap uyuşmazlığı | Otomatik retry yapılmaz; server hesap sonucu gösterilir, kullanıcı miktarı yeniden seçer. |
| `QUANTITY_CONCURRENCY_CONFLICT` | Geçici yarış/elde kalan miktarın değişmesi | Güncel kaynak yeniden okunur; aynı payload körlemesine tekrar gönderilmez. Kullanıcıya yeni kalan miktar gösterilir. |

## 2. Standart API hata yanıtı

Tüm hata yanıtları `application/problem+json` içerik tipiyle dönmelidir. Başarı response'larında da `request_id` ve gerekirse `idempotency_key` correlation için tutulur; hata response'unda hassas SQL, stack trace veya başka kullanıcının verisi dönmez.

### 2.1 Genel schema

```json
{
  "type": "https://erp.local/problems/quantity-base-mismatch",
  "title": "Miktar temel birimle eşleşmiyor",
  "status": 422,
  "code": "QUANTITY_BASE_MISMATCH",
  "detail": "Sunucu hesaplaması ile istemcinin gönderdiği temel miktar aynı değil.",
  "instance": "/api/delivery-notes/7e4.../issue",
  "request_id": "req_01J...",
  "correlation_id": "corr_01J...",
  "occurred_at": "2026-08-16T09:30:00Z",
  "retryable": false,
  "retry_after_seconds": null,
  "source": {
    "entity_type": "DeliveryNoteItem",
    "entity_id": "4d2...",
    "field": "quantity_base",
    "source_id": "sales-order-item-..."
  },
  "quantity": {
    "entered_quantity": 5,
    "entered_packaging_id": "case-packaging-...",
    "entered_packaging_name": "Koli",
    "client_quantity_base": 9500,
    "server_quantity_base": 10000,
    "base_uom": "Adet",
    "quantity_delta": -500,
    "packaging_version": "2026-01-01T00:00:00Z"
  },
  "errors": [
    {
      "code": "QUANTITY_BASE_MISMATCH",
      "field": "quantity_base",
      "message": "5 Koli, geçerli ambalaj katsayısına göre 10.000 Adet olmalıdır.",
      "rejected_value": 9500,
      "expected_value": 10000
    }
  ],
  "current_resource": null,
  "actions": [
    "Ambalaj seviyesini ve miktarı yeniden seçin.",
    "Sunucunun hesapladığı temel miktarı kabul ederek tekrar önizleme alın."
  ]
}
```

Alanların anlamı şöyledir:

| Alan | Zorunluluk | Açıklama |
|---|---:|---|
| `type` | Evet | Hata türünün stabil URI'si; istemci davranışını yalnızca URI metnine bağlamamak için `code` da gönderilir. |
| `title` | Evet | Kullanıcıya uygun kısa hata başlığı. |
| `status` | Evet | HTTP durum kodu. |
| `code` | Evet | Makine tarafından işlenecek stabil ERP hata kodu. |
| `detail` | Evet | Güvenli, bağlama özel açıklama. |
| `instance` | Evet | Hatanın oluştuğu endpoint/resource yolu. |
| `request_id` | Evet | Tek API isteğinin izleme kimliği. |
| `correlation_id` | Önerilir | Mobil preview → commit veya web ekranı gibi çok adımlı akışın ortak kimliği. |
| `retryable` | Evet | Otomatik tekrar gönderimin güvenli olup olmadığı. |
| `retry_after_seconds` | Koşullu | Sadece geçici/tekrar denenebilir hatalarda. |
| `source` | Koşullu | Kaynak entity, kalem ve hatalı alan. |
| `quantity` | Koşullu | Miktar uyuşmazlığında client/server karşılaştırması. |
| `current_resource` | Koşullu | Concurrency hatasında güncel kalan projection. |
| `errors` | Evet | Bir veya daha fazla alan/kural hatası. |
| `actions` | Önerilir | Kullanıcı veya istemci için güvenli sonraki adımlar. |

## 3. QUANTITY_BASE_MISMATCH mekanizması

### 3.1 Ne zaman üretilir?

`QUANTITY_BASE_MISMATCH`, istemcinin bildirdiği temel miktarın server-side hesaplanan miktardan farklı olması veya miktar dönüşümünün işlem bağlamıyla uyumsuz olması halinde üretilir.

Kontroller şunlardır:

| Kontrol | Örnek hata kodu |
|---|---|
| `entered_packaging_id` ürünle eşleşmiyor | `PACKAGING_PRODUCT_MISMATCH` |
| Ambalaj version işlem tarihi için geçerli değil | `PACKAGING_VERSION_INVALID` |
| `entered_quantity × quantity_in_base_uom` ile client `quantity_base` farklı | `QUANTITY_BASE_MISMATCH` |
| UOM precision aşılıyor | `QUANTITY_PRECISION_EXCEEDED` |
| `allow_partial = false` olan ambalaj parçalı giriliyor | `PACKAGING_PARTIAL_NOT_ALLOWED` |
| Kırılım toplamı temel miktara eşit değil | `PACKAGING_BREAKDOWN_MISMATCH` |

### 3.2 HTTP ve istemci davranışı

| Durum | Değer |
|---|---|
| HTTP status | `422 Unprocessable Entity` |
| `code` | `QUANTITY_BASE_MISMATCH` |
| `retryable` | `false` |
| Otomatik retry | Yapılmaz |
| Stok/rezervasyon/cari etkisi | Yok |
| Audit | Validation failure olarak yazılabilir; ledger yazılmaz |
| UI | Girilen ambalaj ve server hesap sonucu yan yana gösterilir |

İstemci önce `quantity-preview` endpoint'ini çağırabilir. Preview yalnızca hesap ve validation sonucu üretir; allocation, stok hareketi, cari hareket ve state transition oluşturmaz. Commit endpoint'i preview sonucunu tekrar server-side hesaplar. Preview ile commit arasında packaging veya kalan miktar değişmişse yeni hata sözleşmesi döner; istemci preview'ı geçersiz kabul edip yeniden alır.

### 3.3 Örnek endpoint yanıtı

```http
POST /api/delivery-notes/{deliveryNoteId}/issue
Idempotency-Key: 01JQTY-issue-001
X-Correlation-Id: corr-01JQTY
Content-Type: application/json
```

```json
{
  "items": [
    {
      "delivery_note_item_id": "dni-001",
      "entered_quantity": 5,
      "entered_packaging_id": "koli-001",
      "quantity_base": 9500
    }
  ]
}
```

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json
X-Request-Id: req-01JQTY
```

```json
{
  "type": "https://erp.local/problems/quantity-base-mismatch",
  "title": "Miktar temel birimle eşleşmiyor",
  "status": 422,
  "code": "QUANTITY_BASE_MISMATCH",
  "detail": "dni-001 için sunucu 10.000 Adet hesapladı; 9.500 Adet gönderildi.",
  "instance": "/api/delivery-notes/dn-001/issue",
  "request_id": "req-01JQTY",
  "correlation_id": "corr-01JQTY",
  "retryable": false,
  "quantity": {
    "entered_quantity": 5,
    "entered_packaging_id": "koli-001",
    "client_quantity_base": 9500,
    "server_quantity_base": 10000,
    "base_uom": "Adet"
  },
  "errors": [
    {
      "code": "QUANTITY_BASE_MISMATCH",
      "field": "items[0].quantity_base",
      "message": "Server-side hesaplanan değerle eşleşmiyor."
    }
  ],
  "actions": ["quantity-preview endpoint'ini yeniden çağırın", "miktarı server sonucu ile güncelleyin"]
}
```

## 4. QUANTITY_CONCURRENCY_CONFLICT mekanizması

### 4.1 Ne zaman üretilir?

`QUANTITY_CONCURRENCY_CONFLICT`, commit başında görülen kalan miktar ile kaynak satır kilitlendikten sonra yeniden okunan güncel miktar arasında fark oluştuğunda ve yeni talep artık üst sınırı aştığında üretilir.

Örnek: Kalan sevk miktarı 600 adetken iki kullanıcı aynı anda 400 adet sevk etmeye çalışır. İlk transaction 400 adedi commit edince kalan 200 olur. İkinci transaction güncel satırı okuduğunda kendi 400 adet talebi üst sınırı aşar ve kontrollü şekilde reddedilir.

### 4.2 HTTP ve istemci davranışı

| Durum | Değer |
|---|---|
| HTTP status | `409 Conflict` |
| `code` | `QUANTITY_CONCURRENCY_CONFLICT` |
| `retryable` | `true`, ancak aynı payload körlemesine tekrar gönderilmez |
| `Retry-After` | Genellikle gönderilmez; kısa lock beklemesi varsa saniye cinsinden gönderilebilir |
| Stok/rezervasyon/cari etkisi | Başarısız transaction için yok |
| Audit | Conflict event ve kaynak version bilgisi yazılır |
| UI | Güncel kalan miktar, son işlemi yapan kullanıcı/zaman ve yeniden hesapla aksiyonu gösterilir |

İstemci hatadan sonra önce ilgili resource'u yeniden GET eder veya kontrollü `refresh` çağrısı yapar. Kullanıcı yeni kalan miktarı onaylamadan otomatik miktar düşürüp commit etmez. Güvenli otomasyon yalnızca ürün policy'si ve kullanıcı yetkisi açıkça izin veriyorsa uygulanabilir.

### 4.3 Örnek hata yanıtı

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
X-Request-Id: req-01JCONFLICT
```

```json
{
  "type": "https://erp.local/problems/quantity-concurrency-conflict",
  "title": "Miktar başka bir işlem tarafından değiştirildi",
  "status": 409,
  "code": "QUANTITY_CONCURRENCY_CONFLICT",
  "detail": "Bu sevk kaleminde talep edilen miktar için güncel kalan miktar yeterli değil.",
  "instance": "/api/delivery-notes/dn-001/issue",
  "request_id": "req-01JCONFLICT",
  "correlation_id": "corr-01JQTY",
  "retryable": true,
  "retry_after_seconds": null,
  "source": {
    "entity_type": "SalesOrderItem",
    "entity_id": "soi-001",
    "field": "remaining_qty",
    "version_at_request": 17,
    "current_version": 18
  },
  "quantity": {
    "requested_quantity_base": 400,
    "remaining_before_request": 600,
    "remaining_after_other_commit": 200,
    "current_shipped_qty": 9800,
    "current_remaining_qty": 200,
    "base_uom": "Adet"
  },
  "current_resource": {
    "resource_type": "SalesOrderItem",
    "resource_id": "soi-001",
    "state": "PartiallyShipped",
    "remaining_qty": 200,
    "row_version": 18,
    "last_changed_at": "2026-08-16T09:32:00Z"
  },
  "errors": [
    {
      "code": "QUANTITY_CONCURRENCY_CONFLICT",
      "field": "items[0].quantity_base",
      "message": "Talep edilen 400 Adet, güncel kalan 200 Adedi aşıyor."
    }
  ],
  "actions": [
    "Kaynağı yenileyin",
    "Güncel kalan miktarı kullanıcıya gösterin",
    "Kullanıcı onayından sonra yeni commit isteği oluşturun"
  ]
}
```

## 5. Ortak hata pipeline'ı

API katmanında hata yönetimi aşağıdaki sırayla çalışmalıdır:

```text
HTTP request
→ request/correlation/idempotency context oluştur
→ authentication + authorization
→ command validation
→ application/domain rule
→ transaction + row lock
→ PostgreSQL constraint veya concurrency exception
→ exception mapping
→ safe ProblemDetails response
→ structured log + audit/conflict event
```

Exception mapping, veritabanı mesajını doğrudan dışarı sızdırmaz. `DbUpdateConcurrencyException`, `SerializationFailure` veya application-level version conflict kontrollü biçimde `QUANTITY_CONCURRENCY_CONFLICT` koduna map edilir. Miktar hesap uyuşmazlığı ise `QUANTITY_BASE_MISMATCH` veya daha özel alt kodlardan birine map edilir.

Loglarda `request_id`, `correlation_id`, `idempotency_key` hash'i, entity/source ID, expected/current quantity ve algorithm/version bilgileri tutulabilir. Ham Authorization header, tam ödeme bilgisi, parola, hassas kişisel veri ve stack trace API response'una konulmaz.

## 6. Allocation tablo şemaları

Aşağıdaki SQL, PostgreSQL ön taslağıdır. Gerçek migration yazılmadan önce mevcut UUID, company, audit ve document sequence standartlarıyla birleştirilmelidir.

### 6.1 Ortak enum ve helper alanları

```sql
CREATE TYPE allocation_status AS ENUM (
    'Active',
    'Reversed',
    'Voided'
);

CREATE TYPE allocation_source_type AS ENUM (
    'SalesOrderItem',
    'DeliveryNoteItem'
);
```

Projede enum yerine lookup table kullanma kararı verilirse aynı değerler kod alanı olarak korunur; API sözleşmesi değişmemelidir.

### 6.2 `delivery_note_item_allocations`

```sql
CREATE TABLE delivery_note_item_allocations (
    id uuid PRIMARY KEY,
    company_id uuid NOT NULL,
    sales_order_item_id uuid NOT NULL
        REFERENCES sales_order_items(id),
    delivery_note_item_id uuid NOT NULL
        REFERENCES delivery_note_items(id),
    quantity_base numeric(18,6) NOT NULL
        CHECK (quantity_base > 0),
    base_uom_id uuid NOT NULL
        REFERENCES units_of_measure(id),
    entered_quantity numeric(18,6) NOT NULL
        CHECK (entered_quantity > 0),
    entered_packaging_id uuid NULL
        REFERENCES product_packagings(id),
    packaging_snapshot jsonb NOT NULL,
    quantity_operation_snapshot jsonb NULL,
    status allocation_status NOT NULL DEFAULT 'Active',
    idempotency_key varchar(128) NOT NULL,
    payload_hash varchar(128) NOT NULL,
    reversed_from_id uuid NULL
        REFERENCES delivery_note_item_allocations(id),
    reversal_reason text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by uuid NULL REFERENCES users(id),
    reversed_at timestamptz NULL,
    reversed_by uuid NULL REFERENCES users(id),
    row_version bigint NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX ux_delivery_allocation_active_target
    ON delivery_note_item_allocations(sales_order_item_id, delivery_note_item_id)
    WHERE status = 'Active';

CREATE UNIQUE INDEX ux_delivery_allocation_idempotency
    ON delivery_note_item_allocations(company_id, idempotency_key);

CREATE INDEX ix_delivery_allocation_source
    ON delivery_note_item_allocations(sales_order_item_id, status);

CREATE INDEX ix_delivery_allocation_target
    ON delivery_note_item_allocations(delivery_note_item_id, status);
```

`ux_delivery_allocation_active_target` aynı sipariş kaleminin aynı irsaliye kalemine iki aktif allocation ile bağlanmasını engeller. Aynı kaynak sipariş kalemi farklı irsaliyelere bağlanabilir; kısmi sevkiyatın temel özelliği budur.

### 6.3 `invoice_item_allocations`

```sql
CREATE TABLE invoice_item_allocations (
    id uuid PRIMARY KEY,
    company_id uuid NOT NULL,
    delivery_note_item_id uuid NOT NULL
        REFERENCES delivery_note_items(id),
    invoice_item_id uuid NOT NULL
        REFERENCES invoice_items(id),
    quantity_base numeric(18,6) NOT NULL
        CHECK (quantity_base > 0),
    base_uom_id uuid NOT NULL
        REFERENCES units_of_measure(id),
    entered_quantity numeric(18,6) NOT NULL
        CHECK (entered_quantity > 0),
    entered_packaging_id uuid NULL
        REFERENCES product_packagings(id),
    packaging_snapshot jsonb NOT NULL,
    price_snapshot jsonb NOT NULL,
    tax_snapshot jsonb NOT NULL,
    status allocation_status NOT NULL DEFAULT 'Active',
    idempotency_key varchar(128) NOT NULL,
    payload_hash varchar(128) NOT NULL,
    credited_from_id uuid NULL
        REFERENCES invoice_item_allocations(id),
    credit_reason text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by uuid NULL REFERENCES users(id),
    credited_at timestamptz NULL,
    credited_by uuid NULL REFERENCES users(id),
    row_version bigint NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX ux_invoice_allocation_active_target
    ON invoice_item_allocations(delivery_note_item_id, invoice_item_id)
    WHERE status = 'Active';

CREATE UNIQUE INDEX ux_invoice_allocation_idempotency
    ON invoice_item_allocations(company_id, idempotency_key);

CREATE INDEX ix_invoice_allocation_source
    ON invoice_item_allocations(delivery_note_item_id, status);

CREATE INDEX ix_invoice_allocation_target
    ON invoice_item_allocations(invoice_item_id, status);
```

Bir irsaliye kaleminin farklı fatura kalemlerine bölünebilmesi için unique anahtar kaynak kalem tek başına değil, kaynak + hedef belge kalemi çiftidir. Böylece `DeliveryNoteItem A` için `InvoiceItem 1` ve `InvoiceItem 2` ayrı aktif allocation olabilir; aynı hedef invoice item'e ikinci kez allocation yapılamaz.

### 6.4 Projection ve constraint notu

Toplam allocation sınırı PostgreSQL `CHECK` constraint ile doğrudan başka tablo toplamına bağlanamaz. Bu nedenle iki katmanlı koruma gerekir:

1. Application/domain transaction kaynak kalemini `SELECT ... FOR UPDATE` ile kilitler ve aktif allocation toplamını yeniden hesaplar.
2. Database trigger veya deferred constraint trigger, migration kararı seçilirse aynı üst sınırı transaction commit öncesi son kez kontrol eder.

Özet alanlar için önerilen kolonlar şöyledir:

```sql
ALTER TABLE sales_order_items
    ADD CONSTRAINT ck_sales_order_item_quantities_non_negative
    CHECK (
        ordered_qty >= 0 AND
        reserved_qty >= 0 AND
        shipped_qty >= 0 AND
        cancelled_qty >= 0 AND
        shipped_qty + cancelled_qty <= ordered_qty
    );

ALTER TABLE delivery_note_items
    ADD CONSTRAINT ck_delivery_note_item_quantities_non_negative
    CHECK (
        shipped_qty >= 0 AND
        invoiced_qty >= 0 AND
        waived_qty >= 0 AND
        invoiced_qty + waived_qty <= shipped_qty
    );
```

## 7. Örnek SQL sorguları

### 7.1 Sipariş kaleminin kalan sevk miktarı

```sql
SELECT
    soi.id,
    soi.product_id,
    soi.ordered_qty,
    soi.cancelled_qty,
    COALESCE(SUM(
        CASE WHEN dna.status = 'Active'
             THEN dna.quantity_base ELSE 0 END
    ), 0) AS allocated_shipped_qty,
    soi.ordered_qty
      - soi.cancelled_qty
      - COALESCE(SUM(
          CASE WHEN dna.status = 'Active'
               THEN dna.quantity_base ELSE 0 END
        ), 0) AS remaining_qty
FROM sales_order_items soi
LEFT JOIN delivery_note_item_allocations dna
       ON dna.sales_order_item_id = soi.id
WHERE soi.id = :sales_order_item_id
GROUP BY soi.id, soi.product_id, soi.ordered_qty, soi.cancelled_qty;
```

### 7.2 Sevkiyat kesinleştirmeden önce kilitli kalan miktar

```sql
BEGIN;

SELECT id, ordered_qty, cancelled_qty, shipped_qty, row_version
FROM sales_order_items
WHERE id = :sales_order_item_id
FOR UPDATE;

SELECT COALESCE(SUM(quantity_base), 0) AS active_allocated_qty
FROM delivery_note_item_allocations
WHERE sales_order_item_id = :sales_order_item_id
  AND status = 'Active';

-- Application layer burada:
-- requested_qty <= ordered_qty - cancelled_qty - active_allocated_qty
-- kontrolünü kilit altında yapar.

COMMIT;
```

### 7.3 İrsaliye kaleminin faturalanabilir miktarı

```sql
SELECT
    dni.id AS delivery_note_item_id,
    dni.shipped_qty,
    COALESCE(SUM(
        CASE WHEN ina.status = 'Active'
             THEN ina.quantity_base ELSE 0 END
    ), 0) AS invoiced_qty,
    COALESCE(SUM(
        CASE WHEN ina.status = 'Voided'
             THEN ina.quantity_base ELSE 0 END
    ), 0) AS voided_qty,
    dni.shipped_qty
      - COALESCE(SUM(
          CASE WHEN ina.status = 'Active'
               THEN ina.quantity_base ELSE 0 END
        ), 0)
      - dni.waived_qty AS remaining_to_invoice
FROM delivery_note_items dni
LEFT JOIN invoice_item_allocations ina
       ON ina.delivery_note_item_id = dni.id
WHERE dni.id = :delivery_note_item_id
GROUP BY dni.id, dni.shipped_qty, dni.waived_qty;
```

### 7.4 Fatura taslağının allocation özeti

```sql
SELECT
    i.id AS invoice_id,
    i.invoice_number,
    ii.id AS invoice_item_id,
    ii.product_id,
    SUM(ina.quantity_base) FILTER (WHERE ina.status = 'Active')
        AS allocated_qty_base,
    COUNT(ina.id) FILTER (WHERE ina.status = 'Active')
        AS allocation_count
FROM invoices i
JOIN invoice_items ii ON ii.invoice_id = i.id
LEFT JOIN invoice_item_allocations ina
       ON ina.invoice_item_id = ii.id
WHERE i.id = :invoice_id
GROUP BY i.id, i.invoice_number, ii.id, ii.product_id;
```

### 7.5 Aynı idempotency key ile çakışan payload bulma

```sql
SELECT id, endpoint, idempotency_key, payload_hash, response_status,
       created_at, completed_at
FROM idempotency_records
WHERE company_id = :company_id
  AND endpoint = :endpoint
  AND idempotency_key = :idempotency_key;
```

Kayıt varsa payload hash aynıysa ilk response tekrar döndürülür. Hash farklıysa API `409 Conflict` ve `IDEMPOTENCY_PAYLOAD_MISMATCH` döndürür.

### 7.6 Faturalandırılmamış sevkleri raporlama

```sql
SELECT
    dn.document_number AS delivery_note_number,
    dn.issued_at,
    c.name AS customer_name,
    dni.product_id,
    dni.shipped_qty,
    COALESCE(SUM(ina.quantity_base)
        FILTER (WHERE ina.status = 'Active'), 0) AS invoiced_qty,
    dni.shipped_qty
      - COALESCE(SUM(ina.quantity_base)
          FILTER (WHERE ina.status = 'Active'), 0)
      - dni.waived_qty AS remaining_to_invoice
FROM delivery_notes dn
JOIN customers c ON c.id = dn.customer_id
JOIN delivery_note_items dni ON dni.delivery_note_id = dn.id
LEFT JOIN invoice_item_allocations ina
       ON ina.delivery_note_item_id = dni.id
WHERE dn.status IN ('Issued', 'PartiallyInvoiced')
GROUP BY dn.document_number, dn.issued_at, c.name,
         dni.product_id, dni.shipped_qty, dni.waived_qty
HAVING dni.shipped_qty
       - COALESCE(SUM(ina.quantity_base)
           FILTER (WHERE ina.status = 'Active'), 0)
       - dni.waived_qty > 0
ORDER BY dn.issued_at;
```

## 8. Tasarım ve uygulama kabul kriterleri

- `QUANTITY_BASE_MISMATCH` durumunda HTTP `422`, `retryable=false`, server hesap sonucu ve güvenli düzeltme aksiyonları dönmelidir.
- `QUANTITY_CONCURRENCY_CONFLICT` durumunda HTTP `409`, güncel row version ve kalan miktar dönmelidir.
- Aynı idempotency key ve aynı payload ilk başarılı/başarısız sonucu tekrar üretmelidir; ikinci stok veya cari hareketi oluşmamalıdır.
- Aynı key farklı payload ile kullanılırsa `IDEMPOTENCY_PAYLOAD_MISMATCH` dönmelidir.
- Sevk allocation toplamı siparişin iptal edilmemiş miktarını aşmamalıdır.
- Fatura allocation toplamı sevk edilmiş miktarı aşmamalıdır.
- `Invoice.Issued` olmadan `CurrentTransaction(Debit)` oluşmamalıdır.
- Fatura işlemi `StockMovement` üretmemelidir.
- Kesinleşmiş allocation doğrudan update/delete edilememeli; reversal/credit ile düzeltilmelidir.
- Concurrency testinde aynı kalan miktarı aşan iki komuttan yalnızca biri commit olmalıdır.
- API response'ları SQL, stack trace, token ve hassas kişisel veri sızdırmamalıdır.

## 9. Açık kararlar

Bu teknik sözleşmenin production migration'a dönüşmesinden önce şu seçimler proje sahibi tarafından kapatılmalıdır:

| Konu | Açık seçim |
|---|---|
| Allocation status | PostgreSQL enum mu, lookup table mı? |
| Miktar precision | Ürün/UOM bazlı precision değerleri ve yuvarlama politikası |
| Trigger | Üst sınır için deferred constraint trigger kullanılacak mı? |
| Idempotency retention | Kayıtlar ne kadar süre saklanacak ve ne zaman arşivlenecek? |
| Conflict UX | Kullanıcıya otomatik miktar düşürme önerilecek mi, yoksa yalnızca yeniden yükleme mi? |
| Close/waiver | Faturalanmayan sevk miktarı hangi rol ve gerekçeyle kapatılacak? |

Bu belge O-002 ve O-003 için ayrıntılı teknik tasarım önerisidir; karar sahibi onayı gelmeden `DECIDED` olarak işaretlenmemelidir.
