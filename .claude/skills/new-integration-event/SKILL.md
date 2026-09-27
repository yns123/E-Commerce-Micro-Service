---
name: new-integration-event
description: RabbitMQ üzerinden servisler arası yeni bir event tanımlarken, bir servisten event yayınlarken veya bir servise event handler (dinleyici) eklerken kullan. Sipariş akışı, stok rezervasyonu, bildirim gibi servisler arası her etkileşimde; ayrıca "X olunca Y servisi haberdar olsun" türü her istekte bu adımları takip et.
---

# Integration Event Ekleme

Servisler arası tek iletişim yolu event'lerdir, bu yüzden hatalar sessizce kaybolur: yanlış routing key'le yayınlanan mesaj hiçbir kuyruğa düşmez ve hata da vermez. Aşağıdaki sıra bu tür hataları önlemek için var.

## 1. Önce dokümanı yaz — docs/events.md
Event listesine şunları ekle: routing key, record adı, yayınlayan, dinleyen(ler), JSON payload. Kuyruk bağlantıları tablosunu güncelle.
Adlandırma:
- Routing key: `<yayınlayan-servis>.<varlık>.<olay>`, geçmiş zaman (`order.created`, `stock.reserved`) — emir değil, olmuş bir olgu.
- Record: `<Varlık><Olay>IntegrationEvent` (ör. `StockReservedIntegrationEvent`)

## 2. Contract — src/BuildingBlocks/Contracts
```csharp
public sealed record StockReservedIntegrationEvent(
    Guid OrderId,
    IReadOnlyList<ReservedItem> Items) : IntegrationEvent
{
    public const string RoutingKey = "catalog.stock.reserved";
}
public sealed record ReservedItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
```
Kurallar: sadece primitive/record alanlar, entity referansı yok. Dinleyenin ihtiyaç duyduğu veriyi taşı (dinleyen, yayınlayana HTTP ile soramaz).

## 3. Yayınlama
İş değişikliğini **önce kaydet, sonra yayınla**:
```csharp
order.MarkCreated();
db.Orders.Add(order);
await db.SaveChangesAsync(ct);
await eventBus.PublishAsync(new OrderCreatedIntegrationEvent(...), ct);
```
Tersini yapma: kayıt başarısız olursa yayınlanmış bir "hayalet" event kalır.

## 4. Handler — dinleyen serviste IntegrationEvents/Handlers/
```csharp
public sealed class OrderCreatedHandler(CatalogDbContext db, IEventBus bus, ILogger<OrderCreatedHandler> log)
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent e, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == e.Id, ct))
            return;                                          // zaten işlendi

        // ... iş mantığı (Domain metodlarını çağır) ...

        db.ProcessedMessages.Add(new ProcessedMessage(e.Id));
        await db.SaveChangesAsync(ct);                       // iş + idempotency kaydı birlikte

        await bus.PublishAsync(new StockReservedIntegrationEvent(...), ct);
    }
}
```
- Program.cs: `.AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedHandler>()`
- Handler'da `throw` → retry → sonunda dead-letter. İş kuralı sonucu olan "başarısızlık" (ör. stok yetersiz) exception DEĞİLDİR; onu ilgili "failed" event'i ile bildir.
- Handler içinden başka servise HTTP çağrısı yapma.

## 5. Test (tests/<Servis>.Tests)
En az şu iki test:
1. Mutlu yol: handler beklenen durumu üretiyor ve beklenen event yayınlanıyor (IEventBus için sahte bir implementasyon yaz, yayınlananları listede tutsun).
2. İdempotency: aynı event iki kez işlenince durum bir kez değişiyor ve event bir kez yayınlanıyor.
DbContext için `Microsoft.EntityFrameworkCore.Sqlite` in-memory bağlantı (`DataSource=:memory:`) kullan; hızlıdır ve Docker gerektirmez.
Dikkat: SQLite `rowversion` üretmez. Testte `RowVersion` alanına entity oluşturulurken `Guid.NewGuid().ToByteArray()` gibi bir başlangıç değeri ver ya da test DbContext'inde bu property'yi elle ayarla. Gerçek eşzamanlılık davranışı (rowversion çakışması) SQLite ile test edilmez; o, Faz 7'deki Testcontainers (SQL Server) integration testlerinin işidir.

## 6. Doğrula
```bash
dotnet test Ecommerce.sln
docker compose up --build -d
```
RabbitMQ panelinde (localhost:15672) → Queues: dinleyen kuyruğun Bindings kısmında yeni routing key görünmeli. Akışı tetikle, `docker compose logs -f <dinleyen-servis>` ile işlendiğini gör, `ecommerce.deadletter` kuyruğunun boş kaldığını kontrol et.
