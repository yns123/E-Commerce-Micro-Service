---
name: new-service
description: Bu projeye yeni bir mikroservis (ASP.NET Core Web API veya Worker) eklerken ya da mevcut bir servisin iskeletini (Program.cs, Dockerfile, DbContext, docker-compose kaydı, gateway route'u) kurarken kullan. Faz 0'daki servis iskeletlerini oluştururken ve "yeni bir servis lazım" dendiğinde mutlaka bu adımları takip et.
---

# Yeni Mikroservis Ekleme

Amaç: her servisin aynı şekilde kurulması. Böylece bir servisi anlayan herkes hepsini anlar. Bir adımı atlamak genelde "docker'da açılmıyor" veya "gateway 502 dönüyor" şeklinde geri döner.

## 0. Önce karar ver
- Bu gerçekten ayrı bir servis mi? Kendi verisine sahip ve başka servislerle sadece event üzerinden konuşabiliyorsa evet. Başka bir servisin tablosuna ihtiyaç duyuyorsa büyük ihtimalle o servisin parçasıdır — kullanıcıya sor.
- HTTP endpoint'i var mı? Varsa `webapi` (Controllers ile), yoksa `worker` şablonu.

## 1. Proje oluştur
```bash
dotnet new webapi --use-controllers -n <Ad>.Api -o src/Services/<Ad>/<Ad>.Api
# veya: dotnet new worker -n <Ad>.Worker -o src/Services/<Ad>/<Ad>.Worker
dotnet sln Ecommerce.sln add src/Services/<Ad>/<Ad>.Api
dotnet add src/Services/<Ad>/<Ad>.Api reference src/BuildingBlocks/EventBus src/BuildingBlocks/Contracts
```
Şablondan gelen WeatherForecast dosyalarını sil. OpenAPI/Swagger ekleme (gerekmiyor).

## 2. Klasörler
docs/architecture.md → "Bir servisin iç yapısı" bölümündeki klasörleri oluştur: Controllers, Domain, Data, Dtos, IntegrationEvents/Handlers.

## 3. Program.cs sırası
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<<Ad>DbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));                // SQL Server geç açılırsa diye
builder.Services.AddJwtAuth(builder.Configuration);          // BuildingBlocks
builder.Services.AddEventBus(builder.Configuration, queueName: "<ad>.events");
    // .AddSubscription<XIntegrationEvent, XHandler>();       // dinlediği her event için
builder.Services.AddHealthChecks().AddDbContextCheck<<Ad>DbContext>();

var app = builder.Build();

await app.MigrateAndSeedAsync<<Ad>DbContext>();              // Data/ içinde yaz

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/<ad>/health");

app.Run();
```
`AddDbContextCheck` için `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` paketini ekle.

## 4. DbContext
- Tüketici servisse `ProcessedMessages` DbSet'ini ekle (docs/events.md → İdempotency).
- Entity konfigürasyonları `IEntityTypeConfiguration<T>` sınıflarıyla, `ApplyConfigurationsFromAssembly`.
- `decimal` alanlara `HasPrecision(18, 2)` (SQL Server'da vermezsen EF uyarı verir ve kırpma olabilir).
- Paket: `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Design` (migration için).
- `EnableRetryOnFailure` açıkken elle `BeginTransaction` kullanma; tek `SaveChangesAsync` yeterli. Gerekirse `db.Database.CreateExecutionStrategy()` ile sar.
- İlk migration: `dotnet ef migrations add Initial --project src/Services/<Ad>/<Ad>.Api -o Data/Migrations`

## 5. Dockerfile (build context = repo kökü)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Services/<Ad>/<Ad>.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "<Ad>.Api.dll"]
```
Repo kökünde `.dockerignore` olmalı (`**/bin`, `**/obj`, `.git`, `src/Web`).

## 6. docker-compose
```yaml
  <ad>-api:
    build: { context: ., dockerfile: src/Services/<Ad>/<Ad>.Api/Dockerfile }
    environment:
      ConnectionStrings__Default: Server=sqlserver,1433;Database=<Ad>Db;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;Encrypt=True
      RabbitMq__Host: rabbitmq
      Jwt__Key: ${JWT_KEY}
      Jwt__Issuer: ecommerce
      Jwt__Audience: ecommerce
    depends_on:
      sqlserver: { condition: service_healthy }
      rabbitmq: { condition: service_healthy }
```
Veritabanını elle oluşturma; servis açılışındaki `MigrateAsync()` oluşturur.

## 7. Gateway ve dokümanlar
- HTTP'si varsa Gateway `appsettings.json` → ReverseProxy'ye route + cluster ekle: `/api/<ad>/{**catch-all}` → `http://<ad>-api:8080`
- CLAUDE.md servis tablosuna, docs/architecture.md'ye (servis detayı + compose tablosu) ekle.
- Event yayınlıyor/dinliyorsa `new-integration-event` skill'ine geç.

## 8. Doğrula
```bash
dotnet build Ecommerce.sln
docker compose up --build -d <ad>-api
curl -i http://localhost:8080/api/<ad>/health   # 200 beklenir
```
RabbitMQ panelinde `<ad>.events` kuyruğu görünmeli (dinlediği event varsa).
