namespace Catalog.Api.Domain;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = default!;

    private Product() { }

    public static Product Create(string name, string? description, decimal price, int stock, string? imageUrl)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description, decimal price, int stock, string? imageUrl)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        ImageUrl = imageUrl;
    }
}
