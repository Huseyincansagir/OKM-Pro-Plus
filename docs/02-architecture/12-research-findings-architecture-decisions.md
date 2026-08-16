# Architecture karar araştırma notları — 2026-08-16

## EF Core optimistic concurrency

Kaynak: https://learn.microsoft.com/en-us/ef/core/saving/concurrency

- EF Core optimistic concurrency, concurrency token’ın sorgulanıp izlenmesi ve SaveChanges sırasında original değerle karşılaştırılmasıyla çalışır.
- Çakışma olduğunda update/delete etkilenmiş satır sayısı sıfır olur ve `DbUpdateConcurrencyException` oluşur.
- Uygulama exception’ı yakalayıp kullanıcıya conflict gösterebilir veya yeni database değerlerini okuyarak kontrollü retry yapabilir.
- Provider’a göre database-generated token davranışı değişir; SQL Server `rowversion` genellenebilir bir PostgreSQL stratejisi değildir.
- Uygulama tarafından yönetilen token daha kontrollü olabilir; hangi değişikliklerin conflict üreteceği belirlenebilir.

## PostgreSQL transaction isolation

Kaynak: https://www.postgresql.org/docs/current/transaction-iso.html

- PostgreSQL varsayılan olarak Read Committed kullanır.
- Read Committed altında iki ayrı SELECT aynı transaction içinde farklı committed snapshot görebilir.
- `UPDATE`, `DELETE` ve `SELECT FOR UPDATE` aynı hedef satıra concurrent işlem varsa bekleyebilir; ilk transaction commit ederse ikinci işlem güncel versiyona göre koşulu yeniden değerlendirir.
- Repeatable Read ve Serializable daha güçlü garantiler verir; Serializable serialization failure üretebilir ve uygulama retry politikası gerekir.
- Allocation gibi quantity upper-bound işlemlerinde source row `FOR UPDATE` ile kilitlenmeli, güncel remaining/active allocation transaction içinde yeniden hesaplanmalı; yalnızca ilk SELECT snapshot’ına güvenilmemeli.

## Ön karar etkisi

- EF Core için application-managed `Guid` concurrency token yerine PostgreSQL uyumlu monoton `bigint row_version` trigger stratejisi; HTTP tarafında ETag/If-Match kullanılmalı.
- Allocation command’leri için normal Read Committed + source row `FOR UPDATE` + application re-read + database deferred constraint korunmalı. Her command’ı Serializable yapmak MVP için gereksiz geniş retry/lock davranışı doğurur.
- `DbUpdateConcurrencyException`, PostgreSQL serialization/deadlock/unique constraint hataları canonical problem code’lara map edilmeli; aynı business command körlemesine tekrar edilmemeli.

## EF Core transaction and savepoint

Kaynak: https://learn.microsoft.com/en-us/ef/core/saving/transactions

- Transaction commit ederse operations birlikte uygulanır; rollback olursa transaction içindeki değişiklikler uygulanmaz.
- EF Core mevcut transaction içinde SaveChanges çağrısından önce savepoint oluşturabilir; SaveChanges hatasında savepoint’e dönülebilir.
- Allocation/issue command için tek application transaction gerekir; stock movement, allocation, reservation, current debit/credit ve audit aynı commit sınırında tutulmalıdır.
- Savepoint retry’si yalnızca güvenli, idempotent ve güncel veriyi tekrar okuyabilen command’lerde kullanılmalı; aynı payload körlemesine yeniden yürütülmemelidir.

## Npgsql PostgreSQL concurrency

Kaynak: https://www.npgsql.org/efcore/modeling/concurrency.html

- Npgsql, EF Core optimistic concurrency token modelini destekler.
- PostgreSQL’de SQL Server `rowversion` karşılığı yoktur; Npgsql dokümanı otomatik güncellenen concurrency token için `xmin` system column’ını örnekler.
- `xmin` provider/engine detayına bağımlı olduğundan proje için dış API’de monoton `row_version`/ETag sözleşmesi; EF mapping’de ise seçilecek PostgreSQL stratejisinin integration test ile doğrulanması gerekir.
- Mevcut design’da uygulama görünür `row_version bigint` ve trigger yaklaşımı korunacak; `xmin` alternatif olarak Architecture risk kaydında tutulacak, aynı entity’de iki token birlikte kullanılmayacaktır.

## Domain events ve integration events

Kaynak: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation

- Domain event, aynı domain içinde meydana gelen bir olayı diğer parçaların açıkça fark etmesini sağlar.
- Domain event handler side effect’leri aynı domain transaction’ı içinde yürüyebilir; domain event ile integration event aynı mekanizma değildir.
- Integration event, transaction başarıyla persist edildikten sonra external system/bounded context’e asynchronous gönderilmelidir.
- Bu ERP için stok allocation, invoice debit ve current ledger gibi aynı aggregate/business transaction etkileri synchronous command transaction’ında kalmalı; notification, report rebuild, e-belge adapter ve ileride external messaging için outbox/asynchronous dispatch kullanılmalı.

## Outbox araştırma notu

Search sonuçlarında Microsoft Azure transactional outbox kaynağı bulundu ancak 2026-08-16 tarihinde verilen URL `404` döndü. Bu nedenle outbox kararında doğrudan bu sayfanın ayrıntılı iddiaları kullanılmadı; .NET domain events kaynağındaki “integration event ancak persistence sonrası” ayrımı ve mevcut idempotency/audit tasarımı temel alındı.

## Ön karar etkisi

- MVP’de tam event-bus/microservice kurulmayacak.
- Domain entity event listesi transaction içinde toplanacak.
- Aynı transaction’da güvenilir biçimde tekrar çalışması gereken notification/integration kayıtları için `outbox_messages` tablosu önerilecek.
- Worker, committed outbox kayıtlarını retry/backoff ile publish edecek; `message_id`/consumer idempotency ile duplicate delivery tolere edilecek.
- In-process domain handlers yalnızca local domain side-effect’leri için kullanılacak; HTTP/SMTP/e-belge gibi external çağrılar aggregate transaction’ın ortasında yapılmayacak.

## EF Core backing fields

Kaynak: https://learn.microsoft.com/en-us/ef/core/modeling/backing-field

- EF Core backing field ile property yerine field üzerinden okuma/yazma yapabilir; bu encapsulation ve domain method’larıyla kontrollü mutation için uygundur.
- Private collection backing field ve read-only projection, aggregate child collection’larının controller tarafından doğrudan değiştirilmesini önlemek için uygundur.
- Field/property access mode açıkça seçilmelidir; materialization sırasında field, normal kullanımda property yaklaşımı gerekebilir.
- Allocation, order item ve invoice item child listeleri private backing field olarak map edilmeli; public `IReadOnlyCollection` yalnızca okuma için açılmalıdır.

## PostgreSQL explicit locking

Kaynak: https://www.postgresql.org/docs/current/explicit-locking.html

- `SELECT FOR UPDATE` seçilen satırları transaction sonuna kadar concurrent update/delete/lock işlemlerine karşı kilitler.
- Row-level locks başka satırların okumasını değil, aynı satırdaki writer/locker işlemlerini bloklar.
- Lock’lar normal olarak transaction sonuna kadar tutulur; savepoint rollback sonrası ilgili lock’lar bırakılabilir.
- Deadlock riski olduğundan command’ler ilişkili source rows’ları deterministic sırayla kilitlemeli ve deadlock/serialization hatalarını kontrollü problem code’a map etmelidir.

## Ön karar etkisi

- `SalesOrderItem`, `DeliveryNoteItem` veya invoiceable source row önce `FOR UPDATE` ile kilitlenmeli, sonra kalan miktar ve active allocation toplamı yeniden okunmalıdır.
- Tüm application command’lerini table-level lock veya Serializable yapmak yerine row-level lock + Read Committed baseline korunmalıdır.
- Çoklu source item command’lerinde lock sırası ID/sequence’e göre deterministik olmalıdır; aksi halde deadlock testleri ve retry policy gerekir.
- EF Core private backing fields kullanılacak; child collection mutation yalnızca aggregate method’larıyla yapılacaktır.

## GitHub Actions self-hosted runner security

Kaynak: https://docs.github.com/en/actions/reference/security/secure-use

- GitHub-hosted runner’lar ephemeral/clean VM modeli sunar; self-hosted runner için aynı temiz ortam garantisi yoktur.
- Self-hosted runner, workflow’da çalışan untrusted code tarafından kalıcı biçimde compromise edilebilir; private/internal repository’lerde dahi fork/PR ve secret erişimi dikkatle sınırlandırılmalıdır.
- Runner group, environment secrets, required reviewers, least-privilege `GITHUB_TOKEN` ve mümkünse just-in-time/ephemeral runner önerilir.
- Factory ERP production runner’ı yalnızca private repository, protected branch/tag, environment approval ve ayrı runner group ile kullanılmalıdır; PR code’u production self-hosted runner’da çalıştırılmamalıdır.

## .NET value object

Kaynak: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects

- Value object’lerin identity’si yoktur ve immutable olmalıdır.
- Quantity, UOM ve PackagingSnapshot bu model için uygundur; equality değer üzerinden yapılmalıdır.
- EF Core persistence için value object mapping’i private set/backing field veya owned/complex type yaklaşımıyla yapılabilir.

## Ön karar etkisi

- `Quantity` ve `PackagingSnapshot` immutable value object olarak kesinleştirilecek.
- Zero quantity için aggregate projection alanlarında `decimal`/nullable projection veya ayrı `NonNegativeQuantity` kullanılacak; positive transaction quantity ile karıştırılmayacak.
- Production self-hosted runner yalnızca protected release job’ında kullanılacak; PR/test job’ları GitHub-hosted runner veya izole internal runner’da çalışacak.
