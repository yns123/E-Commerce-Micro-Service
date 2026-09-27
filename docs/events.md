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
}
```
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
