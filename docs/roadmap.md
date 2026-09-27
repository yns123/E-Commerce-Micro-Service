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
- [ ] `BuildingBlocks/Contracts`: IntegrationEvent + docs/events.md'deki tüm event record'ları
- [ ] `BuildingBlocks/EventBus`: IEventBus, RabbitMqEventBus, IIntegrationEventHandler, AddEventBus/AddSubscription, retry + dead-letter
- [ ] `BuildingBlocks`: AddJwtAuth extension'ı, PagedResult
**Doğrulama:** Birim testleri geçer; servisler açıldığında RabbitMQ panelinde exchange'ler ve 3 kuyruk görünür.

## Faz 2 — Identity
- [ ] Users tablosu, migration, admin seed
- [ ] register / login / me
- [ ] `identity.user.registered` yayınla; Notification bunu dinleyip loglasın
**Doğrulama:** curl ile kayıt → giriş → token ile /me. `docker compose logs notification-worker` içinde hoş geldin e-postası logu.

## Faz 3 — Catalog
- [ ] Products tablosu, migration, 10 ürün seed
- [ ] Liste (sayfalı + arama), detay, Admin CRUD
**Doğrulama:** Token'sız POST → 401, müşteri token'ıyla → 403, admin token'ıyla → 201.

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
