# Yol Haritası

Her faz tek başına çalışır durumda bitmelidir. Bir faz bitince kutucuğunu işaretle, kısa bir "Notlar" satırı ekle ve dur.

## Faz 0 — İskelet
- [x] `Ecommerce.sln`, `.gitignore` (dotnet), `.env.example`
- [x] docker-compose: sqlserver (healthcheck + volume), rabbitmq (healthcheck)
- [x] Identity/Catalog/Ordering API projeleri: sadece `/api/<servis>/health`
- [x] Notification.Worker: açılınca "Notification worker started" loglar
- [x] Gateway (YARP) route'ları
- [x] Web: nginx + "Merhaba" yazan index.html, `/api/` proxy'si
**Doğrulama:** `docker compose up --build -d` → `curl localhost:8080/api/catalog/health` 200 döner (identity ve ordering için de).

**Notlar:** `dotnet new sln` .NET 10'da varsayılan olarak `.slnx` üretiyor; CLAUDE.md'deki `.sln` kararına uymak için `-f sln` ile klasik format üretildi. Bu fazda servisler henüz DB/JWT/EventBus'a bağlanmıyor (sadece `/health`); `ConnectionStrings`/`Jwt`/`RabbitMq` env değişkenleri docker-compose'a mimari dokümana uygun olarak şimdiden eklendi, ileriki fazlarda kullanılacak.

## Faz 1 — EventBus
- [x] `BuildingBlocks/Contracts`: IntegrationEvent + docs/events.md'deki tüm event record'ları
- [x] `BuildingBlocks/EventBus`: IEventBus, RabbitMqEventBus, IIntegrationEventHandler, AddEventBus/AddSubscription, retry + dead-letter
- [x] `BuildingBlocks`: AddJwtAuth extension'ı, PagedResult
**Doğrulama:** Birim testleri geçer; servisler açıldığında RabbitMQ panelinde exchange'ler ve 3 kuyruk görünür.

**Notlar:** `AddJwtAuth`/`PagedResult` için docs/architecture.md'deki diyagramda ayrıca listelenmemiş yeni bir proje açıldı: `src/BuildingBlocks/Common` (dokümana eklendi). `AddEventBus(config, queueName)` her çağrıldığında kuyruğu hemen declare ediyor (bağlı routing key olmasa bile); henüz hiçbir event/handler tanımlanmadığından şu an Catalog/Ordering/Notification kuyrukları boş bağlı — bu üçü `AddEventBus`'ı çağırdı, Identity ve Gateway Faz 1'de dokunulmadı (Identity, `identity.user.registered`'ı yayınlamaya başladığında Faz 2'de kendi kuyruğunu ekleyecek). Doğrulandı: `dotnet test` → 10/10 geçti; `docker compose up --build -d` sonrası RabbitMQ'da `ecommerce.events` (topic) + `ecommerce.events.dlx` (fanout) exchange'leri ve `catalog.events`, `ordering.events`, `notification.events` + `ecommerce.deadletter` kuyrukları göründü; health endpoint'leri hâlâ 200.

## Faz 2 — Identity
- [x] Users tablosu, migration, admin seed
- [x] register / login / me
- [x] `identity.user.registered` yayınla; Notification bunu dinleyip loglasın
**Doğrulama:** curl ile kayıt → giriş → token ile /me. `docker compose logs notification-worker` içinde hoş geldin e-postası logu.

**Notlar:** İki gerçek sorun bulundu ve düzeltildi:
1. `dotnet ef migrations add` tam Program.cs'i (host builder) çalıştırdığı için `AddJwtAuth`'un fırlattığı "Jwt:Key ayarlanmamış" hatasıyla tasarım zamanında çöküyordu. Çözüm: `Data/IdentityDbContextFactory.cs` içinde `IDesignTimeDbContextFactory<IdentityDbContext>` — migration komutları artık Program.cs'e hiç dokunmuyor, `ConnectionStrings__Default` env var'ı yoksa yerel bir varsayılana düşüyor. Yeni servis eklerken bu dosya da standart hale getirilmeli (new-service skill'ine not düşüldü değil, burada belirtiliyor).
2. `.NET 10`'da record DTO'larda `[property: Required, ...]` hedefi kullanmak çalışma zamanında `InvalidOperationException` fırlatıyor ("validation metadata must be associated with the constructor parameter"). Doğrusu: attribute'u doğrudan constructor parametresine yazmak (`[Required] string Email`, `[property: ...]` DEĞİL). `.claude/skills/new-endpoint/SKILL.md` bu şekilde düzeltildi.

Ayrıca: Identity artık `AddEventBus(config, queueName: "identity.events")` çağırdığı için RabbitMQ'da 4. bir kuyruk (`identity.events`) belirdi — Faz 1'in "3 kuyruk" notuyla tutarlı (o not bunu önceden öngörmüştü). docker-compose sonrası uçtan uca doğrulandı: register (201) → duplicate (409) → kısa şifre (400) → login (200, JWT) → `/me` token'lı (200) / token'sız (401) → yanlış şifre (401) → admin seed girişi çalışıyor → notification-worker loglarında "E-POSTA → ...: Hoş geldiniz!" görünüyor → dead-letter kuyruğu boş.

## Faz 3 — Catalog
- [x] Products tablosu, migration, 10 ürün seed
- [x] Liste (sayfalı + arama), detay, Admin CRUD
**Doğrulama:** Token'sız POST → 401, müşteri token'ıyla → 403, admin token'ıyla → 201.

**Notlar:** `Data/CatalogDbContextFactory.cs` (`IDesignTimeDbContextFactory`) ilk seferden sorunsuz çalıştı — Faz 2'nin dersi işe yaradı. `RowVersion` (`IsRowVersion()`) alanı entity'ye ve migration'a eklendi; kullanım (optimistic concurrency retry) Faz 5'in işi. `ProcessedMessages` tablosu bilinçli olarak eklenmedi — Catalog'un asıl event handler'ı (`ordering.order.created`) Faz 5'te gelecek, o zaman ikinci bir migration ile eklenecek. Doğrulandı: liste (sayfalı+arama), detay, 404, admin PUT/DELETE (204) hepsi çalıştı; token'sız POST → 401, müşteri token'ıyla POST → 403, admin token'ıyla POST → 201 + Location. Not: Gateway varsayılan YARP ayarıyla orijinal `Host` başlığını downstream'e taşımadığı için `Location` başlığındaki adres dıştan değil iç docker adından (`catalog-api:8080`) üretiliyor — işlevi etkilemiyor (frontend id'yi gövdeden okuyacak) ama bilinçli bir sınırlama olarak not düşülüyor, düzeltme istenirse YARP `RequestHeaderOriginalHost` transform'u eklenebilir.

## Faz 4 — Frontend (alışveriş)
- [x] api.js, auth.js, cart.js, layout.js, format.js, style.css
- [x] index, product, cart, login, register sayfaları
**Doğrulama:** Tarayıcıda kayıt ol, giriş yap, ürün ara, sepete ekle, sayfayı yenileyince sepet duruyor.

**Notlar:** `cart.html`'de "Siparişi ver" butonu bilinçli olarak yok — Ordering'in `POST /api/ordering/orders` endpoint'i henüz yazılmadı (Faz 5), sipariş verme akışı Faz 6'nın işi (roadmap'te açıkça orada). `layout.js`'teki header, henüz var olmayan `orders.html`/`admin.html`'e link veriyor (docs/frontend.md'deki sabit header tanımı); bu sayfalar Faz 6'da eklenecek. `api.js`'in 401-yönlendirme davranışı sadece token zaten varken tetikleniyor — token yokken (ör. login'de yanlış şifre) 401 normal bir hata olarak fırlatılıyor, aksi halde login sayfasında sonsuz yönlendirme döngüsü olurdu (docs bu ayrımı açıkça yazmıyor, ama gerekli bir yorum).

Tarayıcıdan uçtan uca doğrulandı: 10 ürün listelendi → arama ("klavye" → 1 sonuç, URL query'ye yansıdı) → ürün detayına git → sepete ekle (başarı mesajı + header sayacı canlı güncellendi) → `cart.html`'de kalem+toplam doğru → sayfa yenilenince sepet duruyor (localStorage) → kayıt ol → login'e yönlendi → giriş yap → `returnUrl`'e (`index.html`) döndü, sepet korundu → header giriş durumuna göre değişti (Admin linki sadece Admin rolünde görünüyor) → çıkış yapınca oturum temizlendi, sepet kaldı. 375px genişlikte yatay kaydırma yok, konsolda hata yok.

## Faz 5 — Sipariş akışı
- [x] Ordering: Orders/OrderItems/ProcessedMessages, POST/GET endpoint'leri
- [x] Catalog: `ordering.order.created` handler'ı (ya hep ya hiç stok rezervasyonu)
- [x] Ordering: stock.reserved / reservation-failed handler'ları
- [x] Notification: confirmed / cancelled handler'ları
- [x] Birim testleri: Product.ReserveStock, Order.Confirm/Cancel, handler idempotency
**Doğrulama:** Stoğu yeterli sipariş → birkaç saniyede Confirmed, stok düşmüş. Stoktan fazla sipariş → Cancelled, stok değişmemiş. Aynı mesaj iki kez işlenince stok iki kez düşmüyor.

**Notlar:**
- Catalog'un `Product.RowVersion` alanı SQL Server'da otomatik üretilen `rowversion`; SQLite'ta (birim testlerinde kullanılan sağlayıcı) otomatik üretilmiyor ve `IsRowVersion()` property'yi store-generated işaretlediği için testte elle atanan değer INSERT'e hiç gitmiyordu (`NOT NULL constraint failed`). Çözüm: `CatalogDbContext.OnModelCreating`'de `!Database.IsSqlServer()` durumunda `RowVersion` için `ValueGeneratedNever()` — sadece test sağlayıcısını etkiliyor, üretimdeki SQL Server davranışı değişmedi. Testte `AddWithRowVersion` yardımcı metoduyla (`tests/Catalog.Tests/TestDbContextFactory.cs`) `Guid.NewGuid().ToByteArray()` atanıyor (new-integration-event skill'inin önerdiği kalıp).
- `OrderCreatedHandler`'daki "ya hep ya hiç" + eşzamanlılık retry: her denemede ürünler yeniden sorgulanıyor (stok yeterliliği taze veriyle kontrol ediliyor), `DbUpdateConcurrencyException` olursa `ChangeTracker` temizlenip en fazla 3 kez yeniden deneniyor; üçüncü denemede de çakışma olursa EventBus'ın kendi retry/dead-letter mekanizmasına düşüyor.
- `Order.Confirm`/`Cancel` domain metodları `Status != Pending` durumunda no-op (idempotency güvencesi domain seviyesinde de var; handler'daki `ProcessedMessages` kontrolü birincil savunma hattı).
- `OrderStatus` enum'u JSON'da string olarak dönüyor (`JsonStringEnumConverter`, Ordering.Api Program.cs'e eklendi — Identity/Catalog'da şu an enum döndüren alan olmadığı için oraya eklenmedi).

Docker'da uçtan uca doğrulandı: yeterli stoklu sipariş (3× Kablosuz Mouse) birkaç saniyede Confirmed oldu, toplam doğru hesaplandı (449.50×3=1348.50), stok 40→37 düştü. Stoktan fazla sipariş (999× Masaüstü Hoparlör, stok 12) Cancelled oldu, `cancelReason` doldu, stok değişmedi. Boş sepet ve `quantity<1` → 400, token'sız → 401. Aynı ürün iki kez sepette → tek kaleme birleşti (miktar toplandı). Başka kullanıcının siparişine erişim → 404 (403 değil, varlık sızdırılmadı). `notification-worker` loglarında hem "onaylandı" hem "iptal edildi" e-postaları göründü, `ecommerce.deadletter` boş kaldı.

## Faz 6 — Frontend (sipariş ve admin)
- [x] cart.html'den sipariş ver, orders.html durum takibi
- [x] admin.html ürün yönetimi
**Doğrulama:** Tarayıcıdan uçtan uca: sepet → sipariş → Pending → Confirmed. Admin olarak ürün ekle, listede görün.

**Notlar:** `cart.html`'e "Siparişi Ver" butonu eklendi (Faz 4'te bilinçli olarak bırakılmıştı); giriş yapmamış kullanıcı butona basınca `login.html?returnUrl=/cart.html`'e yönlendiriliyor (sepet API'si `[Authorize]` olduğu için, misafir sepetinde gezinmeye izin vermeye devam ediyoruz). `orders.html` en fazla 30 saniye boyunca 2 saniyede bir yeniden sorguluyor, Pending kalmayınca duruyor; `?highlight=<orderId>` ile gelen sipariş `order-card--highlight` sınıfıyla vurgulanıyor. `admin.html`'de satır bazlı düzenleme (inline form) ve silme onayı `<dialog>` ile (confirm() kullanılmadı).

Tarayıcıdan uçtan uca doğrulandı: sepete ekle → "Siparişi Ver" → `orders.html?highlight=...`'e yönlendi (test ortamında event işleme çok hızlı olduğu için sayfa yüklendiğinde sipariş zaten Onaylandı durumundaydı, ama curl ile Faz 5'te Pending→Confirmed geçişi ayrıca doğrulanmıştı) → sepet temizlendi. Admin: yeni ürün ekle → listede göründü → satırı düzenle (stok değişti, kaydedildi) → sil (dialog ile onay, listeden kalktı). Customer rolüyle `admin.html`'e gidince `index.html`'e yönlendirildi (`requireAdmin`). 375px genişlikte iki sayfada da yatay kaydırma yok, konsolda hata yok.

**Roadmap tamamlandı** — Faz 0-6 bitti. Kalan tek şey Faz 7 (opsiyonel iyileştirmeler), sadece istenirse yapılacak.

## Faz 7 — Opsiyonel iyileştirmeler (sadece istenirse)
- [x] Transactional Outbox (Ordering ve Catalog için)
- [ ] Integration testleri (WebApplicationFactory + Testcontainers) — istenmedi, yapılmadı
- [x] Structured logging + correlation id (event'lere taşınarak)

**Notlar:**
- **Outbox:** `src/BuildingBlocks/EventBus/Outbox` — `IOutbox.Enqueue<T>(event)` event'i o servisin DbContext'ine ekler (henüz kaydetmez); iş değişikliğiyle **aynı SaveChangesAsync'te** yazılır. Ayrı bir `OutboxDispatcherHostedService<TContext>` 2 saniyede bir işlenmemiş mesajları okuyup gerçek `IEventBus` ile RabbitMQ'ya yayınlıyor. Ordering ve Catalog'un controller/handler'ları `IEventBus` yerine `IOutbox` enjekte ediyor artık; Identity değişmedi (hâlâ doğrudan `IEventBus.PublishAsync`, roadmap'in kapsamı da zaten sadece Ordering+Catalog). Her ikisine de yeni bir `OutboxMessages` tablosu migration'ı eklendi. Docker'da doğrulandı: `OutboxMessages` tablosunda event enqueue edildiği an `ProcessedAt` NULL, dispatcher birkaç saniye içinde dolduruyor (SQL ile sorgulanarak teyit edildi).
- **Correlation id / structured logging:** `IntegrationEvent.CorrelationId` eklendi (bilinçli olarak `set`, `init` değil — bkz. docs/events.md). `Common.Correlation` içindeki `ICorrelationIdAccessor` (AsyncLocal) + `CorrelationIdMiddleware` (Gateway dahil tüm HTTP servislerinde, `X-Correlation-Id` header'ını okur/üretir/forward eder) + EventBus'taki `CorrelationStamper` (her `PublishAsync`/`Enqueue` çağrısında ambient id'yi event'e damgalar) ile bir isteğin/siparişin tüm servis sınırlarını aşan yolculuğu tek id ile izlenebiliyor. `Common.AddStructuredLogging()` konsol logger'ının `ILogger.BeginScope` çıktısını basmasını sağlıyor (`ILoggingBuilder.AddSimpleConsole(o => o.IncludeScopes = true)` — ilk denemede sadece `IServiceCollection.Configure<SimpleConsoleFormatterOptions>` kullanmıştım, hiçbir şey basmadı; `ILoggingBuilder` üzerinden `AddSimpleConsole` çağırmak gerekiyormuş, düzeltildi). Docker'da doğrulandı: `X-Correlation-Id: trace-abc-777` ile sipariş verildi → aynı id, Ordering (istek) → Catalog (stok rezervasyonu) → Ordering (onay) → Notification (e-posta) loglarının hepsinde `=> CorrelationId:trace-abc-777` olarak göründü.
- **Yapılmayan:** Integration testleri (WebApplicationFactory + Testcontainers) kullanıcı tarafından bilinçli olarak istenmedi (yeni bir test altyapısı + Docker-in-test bağımlılığı gerektiriyordu).

Doğrulama sonrası mevcut tüm akışlar (Faz 2-6) yeniden test edildi, regresyon yok: `dotnet build` 0 hata, `dotnet test` 25/25, health endpoint'leri 200, dead-letter boş.
