# Allocation Granularity ve Unique Index Çelişkisi — Çözüm

**Tarih:** 2026-08-16

**Karar:** Bir `SalesOrderItem` ile bir `DeliveryNoteItem` arasındaki ilişki için aynı anda yalnızca bir aktif logical allocation tutulacaktır. Partial shipment, aynı target satırda birden fazla aktif allocation ile değil, farklı `DeliveryNote`/`DeliveryNoteItem` satırlarıyla temsil edilecektir.

## 1. Çelişkinin kaynağı

Mevcut Domain modeli `DeliveryNoteItem` üzerinde allocation toplamını hesaplıyor ve planned/source remaining üst sınırını kontrol ediyor. Mevcut testlerde farklı ID’li ikinci allocation aynı delivery item’a eklenmeye çalışıldığında toplam üst sınırı aşarsa `OVER_ALLOCATION` bekleniyor.

Canonical PostgreSQL tasarımı ise şu kuralı öngörüyor:

```sql
CREATE UNIQUE INDEX ux_delivery_allocation_active_target
    ON delivery_note_item_allocations(sales_order_item_id, delivery_note_item_id)
    WHERE status = 'Active';
```

Bu index aynı source-target çiftinde ikinci aktif allocation’ı quantity toplamı değerlendirilmeden önce engeller. Dolayısıyla Domain’deki “aynı target’a birden fazla aktif allocation olabilir” varsayımı ile database’deki “tek aktif source-target allocation” kuralı aynı anda kullanılamaz.

## 2. Seçilen granularity

`DeliveryNoteItem` tek bir `SalesOrderItem` kaynağını temsil eder. Bu nedenle allocation’ın logical key’i şöyledir:

```text
AllocationLogicalKey = SalesOrderItemId + DeliveryNoteItemId
```

Bir sipariş kalemi farklı irsaliyelere bölünebilir:

```text
SalesOrderItem A
  ├─ DeliveryNoteItem A-1 → one active allocation
  ├─ DeliveryNoteItem A-2 → one active allocation
  └─ DeliveryNoteItem A-3 → one active allocation
```

Aynı delivery item içinde ikinci aktif allocation oluşturulmaz. Böylece:

| İş kuralı | Sonuç |
|---|---|
| Aynı sipariş kalemi farklı irsaliyelerde sevk edilebilir | Evet; target `DeliveryNoteItemId` farklıdır |
| Aynı source-target çiftinde ikinci aktif allocation | Hayır; `DUPLICATE_ALLOCATION` |
| Aynı target satırda miktarın parça parça yazılması | Hayır; target satırın miktarı tek allocation’da temsil edilir |
| Reversal sonrası yeniden sevk | Evet; eski allocation history olarak kalır, yeni aktif allocation oluşturulabilir |
| Farklı command’ın aynı payload’ı | Domain değil, application idempotency store tarafından replay/mismatch yapılır |

Bu seçim O-002’nin “bir siparişten çoklu irsaliye” kuralıyla uyumludur. Partial shipment için yeni delivery note/item oluşturmak, aynı target satırda birden fazla aktif allocation biriktirmekten daha izlenebilir ve canonical index ile uyumludur. [1] [2]

## 3. Domain model değişikliği

### 3.1 Allocation türünü status’ten ayırmak

Reversal satırı pozitif miktarlı bir history kaydıdır; ancak aktif sevk allocation toplamına dahil edilmemelidir. Bu nedenle `AllocationStatus` ile allocation’ın anlamı birbirine karıştırılmamalıdır.

```csharp
public enum AllocationKind
{
    Original = 0,
    Reversal = 1
}

public enum AllocationStatus
{
    Active = 0,
    Reversed = 1,
    Voided = 2
}
```

`DeliveryNoteItemAllocation` içine aşağıdaki property eklenmelidir:

```csharp
public AllocationKind Kind { get; }
public bool IsActiveOriginal
    => Kind == AllocationKind.Original
       && Status == AllocationStatus.Active;
public bool IsReversal
    => Kind == AllocationKind.Reversal;
```

Normal allocation factory’si `Kind = Original` üretir. `CreateReversal` ise `Kind = Reversal` ve `ReversedFromId = original.Id` üretir. `ReversedFromId` yalnızca reversal satırında dolu olmalıdır; original satırın reversal kaydının ID’sini kendi üzerinde taşıması yönsel olarak belirsizlik yaratır.

### 3.2 `DeliveryNoteItem.AddAllocation` kuralları

`AddAllocation` yalnızca normal, aktif allocation kabul etmelidir:

```csharp
public void AddAllocation(
    DeliveryNoteItemAllocation allocation,
    NonNegativeQuantity sourceRemaining,
    DateTimeOffset now)
{
    if (allocation.Kind != AllocationKind.Original
        || allocation.Status != AllocationStatus.Active)
    {
        throw new DomainException(new(
            "ALLOCATION_KIND_INVALID",
            "İrsaliye kalemine yalnızca aktif original allocation eklenebilir."));
    }

    if (allocation.DeliveryNoteItemId != Id
        || allocation.SalesOrderItemId != SalesOrderItemId)
    {
        throw new DomainException(new(
            "ALLOCATION_SCOPE_MISMATCH",
            "Allocation source ve target kalemiyle eşleşmiyor."));
    }

    if (_allocations.Any(x =>
        x.Kind == AllocationKind.Original
        && x.Status == AllocationStatus.Active
        && x.SalesOrderItemId == allocation.SalesOrderItemId
        && x.DeliveryNoteItemId == allocation.DeliveryNoteItemId))
    {
        throw new DomainException(new(
            "DUPLICATE_ALLOCATION",
            "Aynı source-target çifti için aktif allocation zaten mevcut."));
    }

    var activeTotal = ActiveAllocatedQuantity().BaseValue;
    var requested = allocation.QuantityBase.BaseValue;

    if (requested > sourceRemaining.BaseValue
        || activeTotal + requested > QuantityBase.BaseValue)
    {
        throw new DomainException(new(
            "OVER_ALLOCATION",
            "Allocation kaynak kalanını veya planlanan miktarı aşamaz."));
    }

    _allocations.Add(allocation);
    Touch(now);
}
```

`ActiveAllocatedQuantity()` yalnızca `Kind == Original && Status == Active` kayıtlarını toplamalıdır:

```csharp
var total = _allocations
    .Where(x => x.Kind == AllocationKind.Original
             && x.Status == AllocationStatus.Active)
    .Sum(x => x.QuantityBase.BaseValue);
```

### 3.3 Reversal akışı

Reversal ayrı bir active shipment allocation gibi eklenmemelidir. Önerilen akış şöyledir:

```text
active original allocation
    ↓ mark reversed
reversed original allocation
    + positive reversal history row
      Kind = Reversal
      ReversedFromId = original.Id
```

Domain API’si iki açık davranış sağlamalıdır:

```csharp
public void MarkReversed(string reason, DateTimeOffset now)
{
    if (Kind != AllocationKind.Original
        || Status != AllocationStatus.Active)
    {
        throw new DomainException(new(
            "ALLOCATION_NOT_ACTIVE",
            "Yalnızca aktif original allocation tersine çevrilebilir."));
    }

    DomainGuard.AgainstBlank(
        reason,
        "REVERSAL_REASON_REQUIRED",
        "Reversal gerekçesi zorunludur.");

    Status = AllocationStatus.Reversed;
    ReversalReason = reason.Trim();
    Touch(now);
}
```

`CreateReversal` sonucu `Kind = Reversal`, pozitif `QuantityBase` ve `ReversedFromId = original.Id` taşımalıdır. Reversal history satırı aktif original toplamına dahil edilmez. Aggregate seviyesinde `ApplyReversal(original, reversal, now)` gibi tek transaction/domain command metodu kullanılması, original’ın reversed yapılması ile reversal satırının eklenmesini birlikte garanti etmelidir.

Bu değişiklik mevcut testteki “reversal satırı pozitif ve active” beklentisini semantik olarak yeniden adlandırmayı gerektirir. Eğer `Status = Active` korunacaksa bunun “active allocation” anlamına gelmediği `Kind` filtresiyle açıkça belirlenmelidir; daha temiz alternatif, reversal history için ayrı `EntryStatus = Applied` kullanmaktır.

## 4. EF Core konfigürasyonu

### 4.1 Allocation kind mapping ve check constraint

```csharp
builder.Property(x => x.Kind)
    .HasColumnName("allocation_kind")
    .HasMaxLength(20)
    .IsRequired();

builder.ToTable("delivery_note_item_allocations", table =>
{
    table.HasCheckConstraint(
        "ck_delivery_note_allocations_kind",
        "allocation_kind in ('Original', 'Reversal')");

    table.HasCheckConstraint(
        "ck_delivery_note_allocations_quantity_positive",
        "quantity_base > 0");

    table.HasCheckConstraint(
        "ck_delivery_note_allocations_status",
        "status in ('Active', 'Reversed', 'Voided')");
});
```

Enum’ların string olarak map edilmesi için mevcut proje convention’ına göre `HasConversion<string>()` veya explicit value converter kullanılmalıdır. Database ve API’de stabil string değerler korunmalıdır.

### 4.2 Delivery allocation unique index’i

```csharp
builder.HasIndex(x => new
{
    x.SalesOrderItemId,
    x.DeliveryNoteItemId
})
.IsUnique()
.HasDatabaseName("ux_delivery_allocation_active_target")
.HasFilter("status = 'Active' AND allocation_kind = 'Original'");
```

Bu index’in anlamı “sipariş kalemi yalnızca bir irsaliyede kullanılabilir” değildir. Sadece aynı source-target çiftinde aynı anda bir normal aktif allocation bulunabileceğini söyler. Farklı delivery note item’ları farklı target olduğundan partial shipment devam eder.

Aşağıdaki yardımcı index’ler korunmalıdır:

```csharp
builder.HasIndex(x => new { x.SalesOrderItemId, x.Status })
    .HasDatabaseName("ix_delivery_allocation_source_status");

builder.HasIndex(x => new { x.DeliveryNoteItemId, x.Status })
    .HasDatabaseName("ix_delivery_allocation_target_status");
```

### 4.3 Idempotency index’i

Mevcut `builder.HasIndex(x => x.IdempotencyKey).IsUnique()` tüm allocation kayıtları için global benzersizlik yaratır. Bu, aynı key’in farklı endpoint veya operation scope’larında meşru olarak yeniden kullanılabildiği merkezi idempotency sözleşmesiyle uyumsuz olabilir.

İdempotency’nin canonical sahibi `idempotency_records` tablosu olmalıdır:

```sql
CREATE UNIQUE INDEX ux_idempotency_scope_key
    ON idempotency_records(company_scope, endpoint, idempotency_key);
```

Allocation tablosunda key audit amacıyla tutulacaksa şu seçeneklerden biri uygulanmalıdır:

```csharp
// Tercih edilen: merkezi idempotency store unique olur;
// allocation tablosu key üzerinde yalnızca arama index’i taşır.
builder.HasIndex(x => x.IdempotencyKey)
    .HasDatabaseName("ix_delivery_allocation_idempotency_key");
```

veya allocation’ın kendisi operation boundary olacaksa:

```csharp
builder.HasIndex(x => new
{
    x.OperationScope,
    x.IdempotencyKey
})
.IsUnique()
.HasDatabaseName("ux_delivery_allocation_operation_key");
```

İkinci seçenek için `OperationScope`/`CompanyScope` kolonunun entity’ye eklenmesi gerekir. Tek şirketli MVP’de dahi global key unique kuralı, endpoint scope’larının ileride çakışmasına yol açabileceği için doğrudan korunmamalıdır.

### 4.4 Invoice allocation için simetrik kural

Invoice allocation aynı granularity prensibini ters yönde kullanmalıdır:

```text
InvoiceAllocationLogicalKey
    = DeliveryNoteItemId + InvoiceItemId
```

Bir delivery item farklı faturalara bölünebilir; aynı delivery item’ın aynı invoice item’a ikinci aktif allocation’ı olamaz.

```csharp
builder.HasIndex(x => new
{
    x.DeliveryNoteItemId,
    x.InvoiceItemId
})
.IsUnique()
.HasDatabaseName("ux_invoice_allocation_active_target")
.HasFilter("status = 'Active' AND allocation_kind = 'Original'");
```

`DeliveryNoteItemId` tek başına unique yapılmamalıdır; aksi halde O-003 kısmi faturalama ve birden fazla fatura senaryosu bloke edilir. Mevcut `InvoiceItem` üzerindeki `(InvoiceId, DeliveryNoteItemId)` unique index’i, aynı fatura içinde aynı delivery item’ın iki invoice item’a bölünmesini engelleyen ayrı bir belge-line kuralıdır; bu index ile allocation index’i birbirine karıştırılmamalıdır.

## 5. Migration planı

Bu değişiklik uygulanmış migration dosyalarının yeniden yazılmasıyla değil, yeni bir forward-fix migration ile yapılmalıdır.

### 5.1 Ön kontrol

Önce mevcut database’de duplicate aktif source-target kayıtları aranmalıdır:

```sql
SELECT sales_order_item_id,
       delivery_note_item_id,
       COUNT(*) AS active_count
FROM delivery_note_item_allocations
WHERE status = 'Active'
GROUP BY sales_order_item_id, delivery_note_item_id
HAVING COUNT(*) > 1;
```

Invoice tarafı için:

```sql
SELECT delivery_note_item_id,
       invoice_item_id,
       COUNT(*) AS active_count
FROM invoice_item_allocations
WHERE status = 'Active'
GROUP BY delivery_note_item_id, invoice_item_id
HAVING COUNT(*) > 1;
```

Sonuç boş değilse index eklenmemelidir. Kayıtlar quantity birleştirme, bir kaydı void/reversal olarak işaretleme veya business owner onaylı kontrollü backfill ile çözülmelidir. Fiziksel silme yapılmamalıdır.

### 5.2 Forward-fix sırası

```text
1. allocation_kind kolonunu nullable veya güvenli default ile ekle
2. Mevcut kayıtları kontrollü biçimde Original/Reversal olarak sınıflandır
3. AllocationKind check constraint’ini ekle
4. Scope mismatch ve duplicate aktif kayıtları raporla
5. Duplicate’leri audit/reversal/void işlemiyle temizle
6. Partial unique source-target index’lerini ekle
7. Global allocation idempotency unique index’ini kaldır veya scope’lu hale getir
8. EF model snapshot ve integration test fixture’larını güncelle
9. Migration clean-run ve repeat-run testlerini çalıştır
```

`reversed_from_id` mevcut modelde original ve reversal yönünü karıştırmış olabileceği için otomatik `IS NOT NULL ⇒ Reversal` backfill’i körlemesine uygulanmamalıdır. Önce mevcut satırların status, quantity, created order ve referans yönü raporlanmalıdır.

## 6. Test değişiklikleri

### Domain unit tests

Aşağıdaki testler eklenmeli veya güncellenmelidir:

| Senaryo | Beklenen sonuç |
|---|---|
| Aynı source-target için ilk active original allocation | Kabul |
| Aynı source-target için farklı ID’li ikinci active original | `DUPLICATE_ALLOCATION` |
| Aynı source’un farklı delivery item’a allocation’ı | Kabul, source remaining ile sınırlı |
| Exact planned/source boundary | Kabul |
| Boundary aşımı | `OVER_ALLOCATION` |
| Allocation scope başka delivery item’a ait | `ALLOCATION_SCOPE_MISMATCH` |
| Reversal history satırı active toplamına dahil | Asla dahil değil |
| Reversed original sonrasında yeni active allocation | Policy’ye göre kabul |
| Aynı idempotency key, aynı payload | Application replay |
| Aynı idempotency key, farklı payload | Application `IDEMPOTENCY_PAYLOAD_MISMATCH` |

### EF/PostgreSQL integration tests

Gerçek PostgreSQL üzerinde aşağıdaki kanıtlar gerekir:

1. Partial unique index aynı active source-target çifti için duplicate insert’i reddeder.
2. Aynı source farklı target’larda iki allocation’a izin verir.
3. Reversal history satırı partial unique index’e takılmaz ve active original toplamına dahil edilmez.
4. İki connection aynı source remaining’i tüketmeye çalıştığında yalnızca biri commit eder.
5. `quantity_base <= 0` database tarafından reddedilir.
6. Migration ikinci kez çalıştırıldığında duplicate index/constraint üretmez.
7. Central idempotency store aynı key/same payload replay eder, farklı payload’ı reddeder.

## 7. Sonuç

Önerilen çözüm **Option A: source-target başına tek aktif logical allocation** modelidir. Bu model mevcut canonical partial-shipment yaklaşımını korur, çünkü farklı irsaliyeler farklı `DeliveryNoteItem` target’larıdır. Domain’de scope ve active duplicate guard’ı açıkça uygulanmalı; reversal için allocation türü status’ten ayrılmalı; EF Core’da filtered partial unique index `Original + Active` satırlarıyla sınırlandırılmalıdır.

Bu karar uygulanmadan migration yazılmamalıdır. Önce Domain testleri, ardından EF model testleri, sonra gerçek PostgreSQL migration/constraint/concurrency testleri geçmelidir. API, web ve mobil katmanları bu persistence kanıtlarından sonra ilerletilmelidir.

## References

[1]: ./partial-shipment-invoicing-workflow.md "O-002/O-003 Partial Shipment and Invoicing Workflow"
[2]: ./architecture-efcore-and-migration-plan.md "EF Core Entity ve PostgreSQL Migration Architecture"
[3]: ./postgresql-18-migration-sql-specification.md "PostgreSQL 0001–0018 Migration SQL Specification"
[4]: ./quantity-error-handling-and-allocation-sql.md "Quantity Error Handling and Allocation SQL"
[5]: ./implementation-session-001-risk-and-migration.md "Slice 001 Residual Risks and Migration Requirements"


## 8. Implementation progress

Bu kararın ilk uygulama adımı tamamlandı. Aşağıdaki değişiklikler mevcut migration geçmişi yeniden yazılmadan forward-fix olarak uygulandı:

| Alan | Uygulama sonucu |
|---|---|
| Domain | `AllocationKind.Original/Reversal`, source-target scope guard, aktif original duplicate guard ve reversal semantiği eklendi |
| Delivery EF record | `allocation_kind` kolonu, kind check constraint, filtered `(sales_order_item_id, delivery_note_item_id)` unique index’i ve non-unique idempotency audit index’i eklendi |
| Invoice EF record | `allocation_kind` kolonu, kind check constraint, filtered `(delivery_note_item_id, invoice_item_id)` unique index’i ve non-unique idempotency audit index’i eklendi |
| Service writes | Yeni delivery/invoice allocation kayıtları `AllocationKind = Original` ile yazılıyor |
| Migration | `20260816133434_AddAllocationGranularityAndUniqueness` üretildi ve kontrollü Migrator ile `factory_erp_g1` veritabanına uygulandı |
| Backfill | Mevcut allocation satırları default `Original`; mevcut reversal-shaped satırlar `reversed_from_id`/`credited_from_id` ve active status kombinasyonuna göre `Reversal` olarak sınıflandırılıyor |
| Repeatability | Controlled Migrator ikinci kez çalıştırıldı; migration ve seed tekrarında hata/duplicate oluşmadı |
| Live index smoke | Delivery ve invoice active source-target duplicate insert denemeleri rollback-safe PostgreSQL smoke testinde `unique_violation` ile reddedildi |

### 8.1 Doğrulama sonuçları

```text
Domain unit tests: 31/31 PASS
Infrastructure unit tests: 21/21 PASS
Architecture tests: 5/5 PASS
Controlled migration: PASS
Controlled migration repeat-run: PASS
PostgreSQL filtered unique-index smoke: PASS
```

Migration `Down` metodu üretim ledger verisini silmek için kullanılmamalıdır. Uygulanmış production benzeri ortamlarda rollback yerine backup/restore veya yeni forward-fix migration prosedürü kullanılacaktır.

### 8.2 Sonraki teknik adımlar

Bu uygulama source-target benzersizliği ve allocation kind modelini persistence seviyesine taşımıştır. Henüz tamamlanmayan konular; iki gerçek PostgreSQL connection ile concurrent allocation race testi, deferred cross-row upper-bound trigger’ı, allocation reversal işleminin application command transaction’ı, centralized idempotency replay/mismatch integration testi ve row-version/ETag conflict mapping’idir. Bu konular migration/application persistence slice’ının sonraki adımıdır.
