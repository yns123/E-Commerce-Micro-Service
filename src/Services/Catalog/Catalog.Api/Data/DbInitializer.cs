using Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public static class DbInitializer
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await db.Database.MigrateAsync();

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                Product.Create("Kablosuz Kulaklık", "Bluetooth 5.3, aktif gürültü engelleme", 1299.90m, 25, null),
                Product.Create("Mekanik Klavye", "RGB aydınlatmalı, mavi switch", 1899.00m, 15, null),
                Product.Create("Kablosuz Mouse", "Ergonomik tasarım, sessiz tıklama", 449.50m, 40, null),
                Product.Create("27 inç Monitör", "2K çözünürlük, 144Hz yenileme hızı", 6499.00m, 10, null),
                Product.Create("USB-C Hub", "7'si 1 arada, HDMI çıkışlı", 799.00m, 30, null),
                Product.Create("Taşınabilir SSD 1TB", "USB 3.2, 1050MB/s okuma hızı", 2199.00m, 20, null),
                Product.Create("Webcam 1080p", "Otomatik odaklama, dahili mikrofon", 899.90m, 18, null),
                Product.Create("Sırt Çantası", "15.6 inç laptop bölmeli, su geçirmez kumaş", 649.00m, 35, null),
                Product.Create("Akıllı Bileklik", "Nabız ölçer, 7 gün pil ömrü", 999.00m, 22, null),
                Product.Create("Masaüstü Hoparlör", "2.0 kanal, Bluetooth ve AUX girişi", 1149.00m, 12, null));

            await db.SaveChangesAsync();
        }
    }
}
