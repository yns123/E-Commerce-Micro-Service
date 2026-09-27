---
name: new-endpoint
description: Identity, Catalog veya Ordering servisine yeni bir REST endpoint (controller action) eklerken ya da mevcut bir endpoint'i değiştirirken kullan. Yeni DTO, doğrulama, yetkilendirme veya hata yanıtı gerektiren her backend API işinde bu adımları takip et.
---

# Yeni REST Endpoint Ekleme

Kaynak kurallar: docs/api-conventions.md. Bu skill o kuralları uygulamanın sırasını verir.

## 1. Sözleşmeyi dokümana yaz
docs/api-conventions.md → ilgili servisin endpoint tablosuna satır ekle: metod, yol, yetki, istek/yanıt, hata durumları. Frontend bu tabloya göre yazılacak.

## 2. DTO'lar — Dtos/
```csharp
public sealed record CreateProductRequest(
    [property: Required, StringLength(200)] string Name,
    [property: StringLength(2000)] string? Description,
    [property: Range(0.01, 1_000_000)] decimal Price,
    [property: Range(0, int.MaxValue)] int Stock,
    [property: Url] string? ImageUrl);

public sealed record ProductDto(Guid Id, string Name, string? Description, decimal Price, int Stock, string? ImageUrl);
```
Record'larda DataAnnotations için `[property: ...]` hedefini kullan, yoksa doğrulama çalışmaz.
Entity → DTO dönüşümü için entity'de veya DTO'da basit bir statik metod (`ProductDto.From(product)`) yaz; AutoMapper kullanma.

## 3. İş kuralı Domain'de
```csharp
public void UpdatePrice(decimal price)
{
    if (price <= 0) throw new DomainException("Fiyat sıfırdan büyük olmalı.");
    Price = price;
}
```
`DomainException` global exception handler'da 409 ProblemDetails'e çevrilir (bu eşlemeyi BuildingBlocks'ta bir `IExceptionHandler` olarak bir kez yaz).

## 4. Controller
```csharp
[ApiController]
[Route("api/catalog/products")]
public sealed class ProductsController(CatalogDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : ProductDto.From(p);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest req, CancellationToken ct)
    {
        var p = Product.Create(req.Name, req.Description, req.Price, req.Stock, req.ImageUrl);
        db.Products.Add(p);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, ProductDto.From(p));
    }
}
```
Kontrol listesi:
- Okuma sorgularında `AsNoTracking()`
- Liste endpoint'i sayfalı mı (`PagedResult<T>`)?
- Kullanıcıya ait veri mi? Sorguda `UserId == currentUserId` filtresi var mı? Başkasının kaydına 404 dönüyor mu?
- Durum kodları tablodakiyle aynı mı (201 + Location, 202, 204)?

## 5. Dene
```bash
docker compose up --build -d <servis>-api
TOKEN=$(curl -s -X POST localhost:8080/api/identity/login -H 'Content-Type: application/json' \
  -d '{"email":"admin@shop.local","password":"Admin123!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -i -X POST localhost:8080/api/catalog/products -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' -d '{"name":"Test","price":10,"stock":5}'
```
Mutlu yolu ve en az bir hata yolunu (400, 401/403 veya 404) dene.
