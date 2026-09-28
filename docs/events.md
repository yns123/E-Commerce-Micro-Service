# Integration Event Sözleşmeleri

Bu dosya tek doğruluk kaynağıdır. Kod ile bu dosya çelişirse, kod yanlıştır.

## Altyapı
- Exchange: `ecommerce.events` — tip: **topic**, durable
- Dead-letter exchange: `ecommerce.events.dlx` — tip: fanout, durable; bağlı kuyruk: `ecommerce.deadletter`
- Her tüketen servisin **tek bir kuyruğu** var: `<servis>.events` (ör. `catalog.events`), durable, argüman `x-dead-letter-exchange=ecommerce.events.dlx`
- Kuyruk, dinlediği her routing key için exchange'e bind edilir.
- Routing key formatı: `<yayınlayan-servis>.<varlık>.<olay>` — küçük harf, kelimeler tire ile.
- Mesaj gövdesi: JSON (System.Text.Json, camelCase). Özellikler: `persistent = true`, `contentType = application/json`, `messageId = event.Id`, `type = routing key`.
- Exchange/kuyruk tanımları EventBus açılışında idempotent şekilde declare edilir (elle panelden oluşturma yok).

## Temel tip
```csharp
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
```
`CorrelationId` bilinçli olarak `set` (init değil): event nesnesi oluşturulduktan sonra, yayınlanacağı/outbox'a yazılacağı anda EventBus tarafından o anki ambient correlation id ile "damgalanır" (`CorrelationStamper`). Event'i oluşturan kod bu alanı hiç düşünmez.
Her event record'u `src/BuildingBlocks/Contracts` içinde durur ve routing key'ini sabit olarak taşır:
```csharp
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, Guid UserId, string UserEmail,
    IReadOnlyList<OrderItemLine> Items) : IntegrationEvent
{
    public const string RoutingKey = "ordering.order.created";
}
```

## EventBus arayüzü
```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T @event, CancellationToken ct);
}
```
Kayıt: `builder.Services.AddEventBus(builder.Configuration, queueName: "catalog.events").AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedHandler>();`
Tüketici bir `BackgroundService` olarak çalışır; her mesaj için yeni bir DI scope açar (DbContext scoped olduğu için).

## Tüketme kuralları
- Handler başarılı → `BasicAck`.
- Handler exception fırlatırsa → 3 denemeye kadar (aralarında 1s, 2s, 4s bekleme) aynı süreç içinde yeniden dene; hâlâ başarısızsa `BasicNack(requeue: false)` → mesaj dead-letter kuyruğuna düşer ve hata loglanır.
- JSON parse edilemiyorsa yeniden deneme yapma, doğrudan dead-letter.
- `prefetchCount = 10`.
- RabbitMQ açılışta hazır değilse bağlantıyı 5 saniye arayla en fazla 12 kez yeniden dene.

## İdempotency
Veritabanı olan her tüketici servisin DbContext'inde:
```
ProcessedMessages (MessageId Guid PK, ProcessedAt)
```
Handler, iş değişikliklerini ve `ProcessedMessages` kaydını **aynı SaveChangesAsync içinde** yazar. Handler başında MessageId zaten varsa hiçbir şey yapmadan başarılı döner.

## Transactional Outbox (Ordering, Catalog)
Identity dışındaki, event yayınlayan servisler (Ordering, Catalog) event'i doğrudan RabbitMQ'ya yayınlamaz; `IEventBus.PublishAsync` yerine `IOutbox.Enqueue<T>(event)` çağrılır (`src/BuildingBlocks/EventBus/Outbox`). Bu, event'i o servisin kendi DbContext'indeki `OutboxMessages` tablosuna ekler — henüz kaydetmez. İş değişikliği ve outbox kaydı **aynı `SaveChangesAsync` çağrısında**, tek transaction içinde yazılır:
```csharp
db.Orders.Add(order);
outbox.Enqueue(new OrderCreatedIntegrationEvent(...));   // henüz RabbitMQ'ya gitmedi
await db.SaveChangesAsync(ct);                            // ikisi de ya birlikte yazılır ya hiç
```
Ayrı bir `OutboxDispatcherHostedService<TContext>` (`AddOutbox<TContext>()` ile kayıt edilir) 2 saniyede bir işlenmemiş (`ProcessedAt == null`) mesajları okur, gerçek `IEventBus.PublishAsync` ile RabbitMQ'ya yayınlar ve `ProcessedAt`'i doldurur.
```
OutboxMessages (Id Guid PK, Type string, Content string /* JSON */, OccurredAt DateTime, ProcessedAt DateTime?)
```
Kazanç: DB kaydı başarılı olduğu halde event'in hiç yayınlanmaması riski ortadan kalkar (yayınlama birkaç saniye gecikebilir; kabul edilebilir). Identity bu deseni kullanmıyor — `identity.user.registered` hâlâ `SaveChangesAsync` sonrası doğrudan `IEventBus.PublishAsync` ile yayınlanıyor (tek event'i olan basit bir akış, ek karmaşıklığa değmiyor).

## Correlation id ve structured logging
Her `IntegrationEvent`'in `CorrelationId`'si, o event'i tetikleyen HTTP isteğinin (Gateway'den itibaren `X-Correlation-Id` header'ı) ya da tüketilen event'in correlation id'sini taşır — böylece bir siparişin Ordering → Catalog → Ordering → Notification boyunca tüm adımları loglarda tek bir id ile izlenebilir. Mekanizma: `Common` içindeki `ICorrelationIdAccessor` (AsyncLocal tabanlı) o anki ambient id'yi tutar; `CorrelationIdMiddleware` (HTTP servislerinde) header'dan okur/üretir ve `ILogger.BeginScope` ile loglara bağlar; `RabbitMqConsumerHostedService` tüketirken event'in kendi `CorrelationId`'sini ambient değer yapar. `IEventBus.PublishAsync`/`IOutbox.Enqueue` her ikisi de yayınlamadan/outbox'a yazmadan önce event'i ambient id ile damgalar (`CorrelationStamper`) — ambient id yoksa (ör. arka plandaki `OutboxDispatcherHostedService`'in kendi döngüsü) event'in zaten taşıdığı değeri değiştirmez. Konsol logları `SimpleConsoleFormatterOptions.IncludeScopes = true` (`Common.AddStructuredLogging()`) ile scope'ları da basar, böylece `docker compose logs` çıktısında her satırda `=> CorrelationId:...` görünür.

---

## Event listesi

### identity.user.registered
- Record: `UserRegisteredIntegrationEvent`
- Yayınlayan: Identity (kayıt başarılı olunca)
- Dinleyen: Notification (hoş geldin e-postası)
```json
{ "id": "guid", "occurredAt": "ISO-8601", "userId": "guid", "email": "string" }
```

### ordering.order.created
- Record: `OrderCreatedIntegrationEvent`
- Yayınlayan: Ordering (sipariş Pending olarak kaydedilince)
- Dinleyen: Catalog
```json
{ "id": "guid", "occurredAt": "ISO-8601",
  "orderId": "guid", "userId": "guid", "userEmail": "string",
  "items": [ { "productId": "guid", "quantity": 1 } ] }
```

### catalog.stock.reserved
- Record: `StockReservedIntegrationEvent`
- Yayınlayan: Catalog (tüm kalemlerin stoğu düşüldüyse)
- Dinleyen: Ordering → ürün adı/fiyatlarını yazar, Total hesaplar, Status=Confirmed, ardından `ordering.order.confirmed` yayınlar
```json
{ "id": "guid", "occurredAt": "ISO-8601", "orderId": "guid",
  "items": [ { "productId": "guid", "productName": "string", "unitPrice": 0.00, "quantity": 1 } ] }
```

### catalog.stock.reservation-failed
- Record: `StockReservationFailedIntegrationEvent`
- Yayınlayan: Catalog (herhangi bir ürün yoksa veya stok yetersizse; hiçbir stok düşülmez)
- Dinleyen: Ordering → Status=Cancelled, CancelReason=reason, ardından `ordering.order.cancelled` yayınlar
```json
{ "id": "guid", "occurredAt": "ISO-8601", "orderId": "guid", "reason": "string" }
```

### ordering.order.confirmed
- Record: `OrderConfirmedIntegrationEvent`
- Yayınlayan: Ordering
- Dinleyen: Notification
```json
{ "id": "guid", "occurredAt": "ISO-8601", "orderId": "guid", "userEmail": "string", "total": 0.00 }
```

### ordering.order.cancelled
- Record: `OrderCancelledIntegrationEvent`
- Yayınlayan: Ordering
- Dinleyen: Notification
```json
{ "id": "guid", "occurredAt": "ISO-8601", "orderId": "guid", "userEmail": "string", "reason": "string" }
```

## Sipariş akışı
```
Ordering:     POST /api/ordering/orders → Order(Pending) kaydet → publish ordering.order.created → 202 Accepted
Catalog:      ordering.order.created → tüm kalemler için stok yeterli mi?
                evet  → stokları düş → publish catalog.stock.reserved
                hayır → publish catalog.stock.reservation-failed
Ordering:     catalog.stock.reserved → Confirmed → publish ordering.order.confirmed
              catalog.stock.reservation-failed → Cancelled → publish ordering.order.cancelled
Notification: confirmed / cancelled → e-postayı logla
```

## Kuyruk bağlantıları
| Kuyruk | Bind edilen routing key'ler |
|---|---|
| catalog.events | ordering.order.created |
| ordering.events | catalog.stock.reserved, catalog.stock.reservation-failed |
| notification.events | identity.user.registered, ordering.order.confirmed, ordering.order.cancelled |
