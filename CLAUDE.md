# E-Ticaret Mikroservis Projesi

Basit bir e-ticaret uygulaması: ürün listeleme, sepet, sipariş, bildirim.
Amaç öğrenilebilir ve çalışır bir mikroservis mimarisi; "enterprise" karmaşıklığı DEĞİL.
Emin olmadığın bir konuda yeni teknoloji/katman ekleme, sor.

## Teknoloji kararları (değiştirme)
- Backend: .NET 10, ASP.NET Core Web API, **Controller tabanlı** (Minimal API kullanma)
- Veritabanı: SQL Server 2022, EF Core + `Microsoft.EntityFrameworkCore.SqlServer`. Tek SQL Server container, **her servisin ayrı veritabanı** (IdentityDb, CatalogDb, OrderingDb). Bir servis başka servisin veritabanına ASLA bağlanmaz.
- Mesajlaşma: RabbitMQ, resmi `RabbitMQ.Client` (7.x, async API) üzerine yazılmış kendi küçük EventBus kütüphanemiz. MassTransit / NServiceBus KULLANMA.
- API Gateway: YARP (`Yarp.ReverseProxy`)
- Kimlik doğrulama: JWT (HS256). Token'ı Identity servisi üretir, her servis aynı anahtarla kendisi doğrular.
- Şifre hash: `Microsoft.Extensions.Identity.Core` içindeki `PasswordHasher<T>` (ASP.NET Identity'nin tamamını kurma)
- Frontend: saf HTML + CSS + vanilla JavaScript (ES modules). **Hiçbir JS kütüphanesi, framework, npm, build adımı yok.** nginx ile servis edilir.
- Test: xUnit
- Çalıştırma: docker compose

## Servisler
| Servis | Klasör | Sorumluluk |
|---|---|---|
| Identity | src/Services/Identity/Identity.Api | Kayıt, giriş, JWT üretimi |
| Catalog | src/Services/Catalog/Catalog.Api | Ürünler, stok, stok rezervasyonu |
| Ordering | src/Services/Ordering/Ordering.Api | Sipariş oluşturma ve durumu |
| Notification | src/Services/Notification/Notification.Worker | Sadece event dinler, "e-posta"yı loglar. HTTP yok |
| Gateway | src/Gateway/Gateway.Api | YARP ile /api/* isteklerini servislere yönlendirir |
| Web | src/Web | Statik frontend + nginx (/api/* → gateway) |

Detaylar: `docs/architecture.md`

## Altın kurallar
1. Servisler birbirini **HTTP ile çağırmaz**. Servisler arası tek iletişim yolu RabbitMQ event'leridir.
2. Yeni event eklemeden önce `docs/events.md` güncellenir; kod dokümana uyar, tersi değil.
3. Event tüketicileri **idempotent** olmalı (aynı mesaj iki kez gelebilir). Bkz. docs/events.md → İdempotency.
4. İstemciden gelen fiyata asla güvenilmez. Fiyatı Catalog belirler.
5. Her servis tek bir proje: Clean Architecture ile 4 projeye bölme. Klasör yapısı docs/architecture.md'de.
6. Hata yanıtları her zaman ProblemDetails formatında (docs/api-conventions.md).
7. Gizli bilgiler (JWT anahtarı, DB şifresi) koda yazılmaz; environment variable / `.env` ile gelir.

## Ne zaman hangi dosyayı oku
- Yeni servis ekliyorsan → `.claude/skills/new-service/SKILL.md`
- Yeni event yayınlıyor/dinliyorsan → `.claude/skills/new-integration-event/SKILL.md`
- Yeni REST endpoint ekliyorsan → `.claude/skills/new-endpoint/SKILL.md`
- Frontend sayfası yazıyorsan → `.claude/skills/frontend-page/SKILL.md`
- Sıradaki iş ne → `docs/roadmap.md`

## Komutlar
- Her şeyi ayağa kaldır: `docker compose up --build -d`
- Loglar: `docker compose logs -f <servis-adı>`
- Build: `dotnet build Ecommerce.sln`
- Testler: `dotnet test Ecommerce.sln`
- Migration ekle: `dotnet ef migrations add <Ad> --project src/Services/<Servis>/<Servis>.Api`
- Uygulama: http://localhost:8080 — RabbitMQ paneli: http://localhost:15672 (guest/guest)

## "Bitti" tanımı
Bir iş ancak şunların hepsi sağlanınca bitmiştir:
1. `dotnet build` uyarısız/hatasız
2. `dotnet test` geçiyor
3. `docker compose up --build` sonrası ilgili servisin `/health` endpoint'i 200 dönüyor
4. Değişiklik bir akışı etkiliyorsa (ör. sipariş) o akış tarayıcıdan veya curl ile uçtan uca denendi
5. İlgili doküman (events.md, architecture.md, roadmap.md) güncellendi

Bir fazı bitirince docs/roadmap.md'de işaretle ve dur; bir sonraki faza kendiliğinden geçme.
