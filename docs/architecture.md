# Mimari

## Genel akış
```
Tarayıcı ──► nginx (Web, :8080) ──/api/*──► Gateway (YARP) ──► Identity / Catalog / Ordering
                                                                    │         │
                                                                    ▼         ▼
                                                     RabbitMQ (exchange: ecommerce.events)
                                                                    │
                                                                    ▼
                                                           Notification.Worker
```
Frontend ve API aynı origin'den (localhost:8080) sunulur, bu yüzden **CORS gerekmez**, ekleme.

## Solution yapısı
```
Ecommerce.sln
docker-compose.yml
.env.example
infra/
  nginx/default.conf
src/
  BuildingBlocks/
    EventBus/                       # IEventBus, RabbitMqEventBus, IIntegrationEventHandler<T>, IntegrationEvent
    Contracts/                      # Tüm integration event record'ları (servisler bunu referans alır)
  Services/
    Identity/Identity.Api/
    Catalog/Catalog.Api/
    Ordering/Ordering.Api/
    Notification/Notification.Worker/   # Worker Service şablonu (dotnet new worker)
  Gateway/Gateway.Api/
  Web/                              # statik dosyalar + Dockerfile (nginx)
tests/
  Identity.Tests/
  Catalog.Tests/
  Ordering.Tests/
```

## Bir servisin iç yapısı (tek proje, klasörlerle)
```
Catalog.Api/
  Controllers/        # ince controller: doğrulama + iş çağrısı + yanıt
  Domain/             # entity'ler ve iş kuralları (ör. Product.ReserveStock)
  Data/               # DbContext, entity config, Migrations/, SeedData
  Dtos/               # request/response record'ları (entity'yi dışarı açma)
  IntegrationEvents/
    Handlers/         # bu servisin dinlediği event'lerin handler'ları
  Program.cs
  appsettings.json
  Dockerfile
```
İş kuralları Domain içindeki entity metodlarında durur (ör. `order.Confirm(...)`), controller veya handler içinde değil. Ayrı Repository katmanı yazma; DbContext'i doğrudan kullan.

## Servis detayları

### Identity
- Tablo: Users (Id Guid, Email unique, PasswordHash, Role: "Customer" | "Admin", CreatedAt)
- Başlangıçta seed: `admin@shop.local` / `Admin123!` (Role=Admin)
- JWT claim'leri: `sub` (userId), `email`, `role`. Süre: 2 saat.
- Yayınlar: `identity.user.registered`

### Catalog
- Tablo: Products (Id Guid, Name, Description, Price decimal(18,2), Stock int, ImageUrl nullable, CreatedAt)
- Başlangıçta ~10 örnek ürün seed edilir.
- Dinler: `ordering.order.created` → stok rezervasyonu
- Yayınlar: `catalog.stock.reserved`, `catalog.stock.reservation-failed`
- Stok rezervasyonu **ya hep ya hiç**: siparişteki bir kalem bile yetersizse hiçbir stok düşülmez.
- Eşzamanlılık: Product üzerinde `[Timestamp] public byte[] RowVersion` (SQL Server `rowversion`) ile optimistic concurrency; `DbUpdateConcurrencyException` olursa işlemi en fazla 3 kez yeniden dene.

### Ordering
- Tablolar: Orders (Id, UserId, UserEmail, Status, Total, CancelReason nullable, CreatedAt), OrderItems (Id, OrderId, ProductId, ProductName nullable, UnitPrice nullable, Quantity)
- Status: `Pending` → `Confirmed` veya `Cancelled` (başka durum yok)
- Sipariş oluşturulurken sadece productId + quantity bilinir; ProductName ve UnitPrice `catalog.stock.reserved` gelince doldurulur ve Total hesaplanır.
- Dinler: `catalog.stock.reserved`, `catalog.stock.reservation-failed`
- Yayınlar: `ordering.order.created`, `ordering.order.confirmed`, `ordering.order.cancelled`

### Notification
- Veritabanı yok (idempotency için bellek içi son 1000 mesaj Id'si yeterli).
- Dinler: `identity.user.registered`, `ordering.order.confirmed`, `ordering.order.cancelled`
- Gerçek e-posta göndermez; `ILogger` ile `E-POSTA → {email}: {konu}` şeklinde loglar.

### Gateway (YARP)
Route'lar path prefix ile, path dönüştürmeden:
- `/api/identity/{**catch-all}` → http://identity-api:8080
- `/api/catalog/{**catch-all}` → http://catalog-api:8080
- `/api/ordering/{**catch-all}` → http://ordering-api:8080

Gateway kimlik doğrulama yapmaz; sadece yönlendirir. Doğrulamayı servisler yapar.

## docker-compose servisleri
| Servis adı | İmaj / build | Host portu |
|---|---|---|
| sqlserver | mcr.microsoft.com/mssql/server:2022-latest | 1433 |
| rabbitmq | rabbitmq:4-management | 5672, 15672 |
| identity-api | build | — |
| catalog-api | build | — |
| ordering-api | build | — |
| notification-worker | build | — |
| gateway | build | 5000 (debug için) |
| web | build (nginx) | 8080 |

- Tüm .NET container'ları içeride 8080 portunu dinler (.NET varsayılanı).
- .NET servisleri `depends_on` ile sqlserver ve rabbitmq için `condition: service_healthy` bekler; bu yüzden ikisine de healthcheck tanımla.
- SQL Server container'ı: `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD=${SA_PASSWORD}`, `MSSQL_PID=Developer`, veri için named volume (`sqlserver-data:/var/opt/mssql`).
- SQL Server healthcheck (açılışı yavaştır, `start_period` önemli):
  ```yaml
  healthcheck:
    test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$${MSSQL_SA_PASSWORD}\" -C -Q 'SELECT 1' || exit 1"]
    interval: 10s
    timeout: 5s
    retries: 10
    start_period: 30s
  ```
- Veritabanlarını elle oluşturma: EF Core `MigrateAsync()` veritabanı yoksa kendisi oluşturur. Init script gerekmez.
- Her API servisi `/api/<servis>/health` endpoint'i sunar (`AddHealthChecks` + `MapHealthChecks`), böylece gateway üzerinden de erişilir.
- Migration'lar servis açılışında `Database.MigrateAsync()` ile uygulanır; seed data da o sırada eklenir (tablo boşsa).
- Dockerfile'lar multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` ile build, `mcr.microsoft.com/dotnet/aspnet:10.0` ile çalıştır. Build context repo kökü (BuildingBlocks'a erişmek için).

## Konfigürasyon (environment variable)
```
ConnectionStrings__Default=Server=sqlserver,1433;Database=CatalogDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;Encrypt=True
RabbitMq__Host=rabbitmq
RabbitMq__User=guest
RabbitMq__Password=guest
Jwt__Key=${JWT_KEY}          # en az 32 karakter
Jwt__Issuer=ecommerce
Jwt__Audience=ecommerce
```
`.env.example` bu değişkenlerin örnek değerlerini içerir; gerçek `.env` git'e girmez.
`SA_PASSWORD` SQL Server'ın şifre kuralına uymalı: en az 8 karakter, büyük harf + küçük harf + rakam + sembol (ör. `Str0ng!Passw0rd`). Uymazsa container sessizce kapanır.
Basitlik için servisler `sa` ile bağlanır (sadece geliştirme ortamı; bilinçli basitleştirme).

## Bilinçli basitleştirmeler
- Outbox pattern yok: event, `SaveChangesAsync` başarılı olduktan sonra yayınlanır. Nadiren event kaybı olabilir; kabul edildi (roadmap'te opsiyonel faz).
- Ödeme yok: stok rezerve edilince sipariş onaylanır.
- Sepet sunucuda tutulmaz; tarayıcıda localStorage'da durur.
- Refresh token yok; token süresi dolunca kullanıcı tekrar giriş yapar.
- Tüm servisler `sa` kullanıcısıyla bağlanır; servis başına ayrı SQL login yok.
