# API Kuralları

## Genel
- Controller tabanlı: `[ApiController]`, `[Route("api/<servis>/[controller]")]` yerine route'u açıkça yaz (ör. `[Route("api/catalog/products")]`).
- Controller ince olur: DTO al → iş kuralını çağır → DTO döndür. EF entity'si asla doğrudan dönmez.
- Request/response tipleri `Dtos/` altında `record` olarak tanımlanır.
- Doğrulama: DataAnnotations (`[Required]`, `[Range]`, `[EmailAddress]`, `[StringLength]`). `[ApiController]` geçersiz modelde otomatik 400 döner, bunu elle yazma.
- JSON: camelCase, enum'lar string olarak (`JsonStringEnumConverter`).
- Tüm id'ler `Guid`. Tüm zamanlar UTC.
- Async her yerde; `CancellationToken` controller'dan DbContext'e kadar geçirilir.

## Hata formatı: ProblemDetails
`builder.Services.AddProblemDetails()` + `app.UseExceptionHandler()` + `app.UseStatusCodePages()` kullanılır.
| Durum | Kod | Nasıl |
|---|---|---|
| Doğrulama hatası | 400 | otomatik (ValidationProblemDetails) |
| Giriş yok / token geçersiz | 401 | JwtBearer otomatik |
| Yetki yok (ör. Admin değil) | 403 | `[Authorize(Roles = "Admin")]` otomatik |
| Kayıt bulunamadı | 404 | `return NotFound();` |
| İş kuralı ihlali (ör. e-posta zaten kayıtlı) | 409 | `return Problem(statusCode: 409, title: "...", detail: "...")` |
| Beklenmeyen hata | 500 | global exception handler, detay loglanır ama yanıtta gösterilmez |

Frontend hata mesajını `title` (yoksa `detail`) alanından okur; ValidationProblemDetails'te `errors` sözlüğünü gösterir.

## Sayfalama
Liste endpoint'leri: `?page=1&pageSize=20` (pageSize en fazla 100). Yanıt:
```json
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0 }
```
Ortak tip: `PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)`.

## Kimlik doğrulama
- Header: `Authorization: Bearer <token>`
- Kullanıcı id'si: `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` → Guid. `MapInboundClaims = false` ayarla ki claim adları değişmesin; `RoleClaimType = "role"`, `NameClaimType = "email"`.
- JWT doğrulama kodu her serviste aynı; `BuildingBlocks` içinde `AddJwtAuth(IConfiguration)` extension'ı olarak yaz ve tekrar kullan.

## Endpoint listesi

### Identity
| Metod | Yol | Yetki | Açıklama |
|---|---|---|---|
| POST | /api/identity/register | — | `{email, password}` → 201 `{userId}`; e-posta varsa 409. Şifre en az 6 karakter |
| POST | /api/identity/login | — | `{email, password}` → 200 `{token, expiresAt, email, role}`; yanlışsa 401 |
| GET | /api/identity/me | Giriş | `{userId, email, role}` |

### Catalog
| Metod | Yol | Yetki | Açıklama |
|---|---|---|---|
| GET | /api/catalog/products | — | Sayfalı liste; `?search=` ad içinde arar |
| GET | /api/catalog/products/{id} | — | Tek ürün |
| POST | /api/catalog/products | Admin | Oluştur → 201 + Location |
| PUT | /api/catalog/products/{id} | Admin | Güncelle → 204 |
| DELETE | /api/catalog/products/{id} | Admin | Sil → 204 |

Ürün DTO: `{ id, name, description, price, stock, imageUrl }`

### Ordering
| Metod | Yol | Yetki | Açıklama |
|---|---|---|---|
| POST | /api/ordering/orders | Giriş | `{items:[{productId, quantity}]}` → **202** `{orderId, status:"Pending"}`. Boş sepet veya quantity < 1 → 400. Aynı productId birden fazla gelirse miktarları birleştir |
| GET | /api/ordering/orders | Giriş | Sadece giriş yapan kullanıcının siparişleri, yeniden eskiye, sayfalı |
| GET | /api/ordering/orders/{id} | Giriş | Başkasının siparişi ise 404 (403 değil — varlığını sızdırma) |

Sipariş DTO: `{ id, status, total, cancelReason, createdAt, items:[{productId, productName, unitPrice, quantity}] }`
