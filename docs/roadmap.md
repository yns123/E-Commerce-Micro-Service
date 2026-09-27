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
- [ ] api.js, auth.js, cart.js, layout.js, format.js, style.css
- [ ] index, product, cart, login, register sayfaları
**Doğrulama:** Tarayıcıda kayıt ol, giriş yap, ürün ara, sepete ekle, sayfayı yenileyince sepet duruyor.

## Faz 5 — Sipariş akışı
- [ ] Ordering: Orders/OrderItems/ProcessedMessages, POST/GET endpoint'leri
- [ ] Catalog: `ordering.order.created` handler'ı (ya hep ya hiç stok rezervasyonu)
- [ ] Ordering: stock.reserved / reservation-failed handler'ları
- [ ] Notification: confirmed / cancelled handler'ları
- [ ] Birim testleri: Product.ReserveStock, Order.Confirm/Cancel, handler idempotency
**Doğrulama:** Stoğu yeterli sipariş → birkaç saniyede Confirmed, stok düşmüş. Stoktan fazla sipariş → Cancelled, stok değişmemiş. Aynı mesaj iki kez işlenince stok iki kez düşmüyor.

## Faz 6 — Frontend (sipariş ve admin)
- [ ] cart.html'den sipariş ver, orders.html durum takibi
- [ ] admin.html ürün yönetimi
**Doğrulama:** Tarayıcıdan uçtan uca: sepet → sipariş → Pending → Confirmed. Admin olarak ürün ekle, listede görün.

## Faz 7 — Opsiyonel iyileştirmeler (sadece istenirse)
- [ ] Transactional Outbox (Ordering ve Catalog için)
- [ ] Integration testleri (WebApplicationFactory + Testcontainers)
- [ ] Structured logging + correlation id (event'lere taşınarak)
