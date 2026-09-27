using System.ComponentModel.DataAnnotations;
using Catalog.Api.Domain;

namespace Catalog.Api.Dtos;

public sealed record CreateProductRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(0.01, 1_000_000)] decimal Price,
    [Range(0, int.MaxValue)] int Stock,
    [Url] string? ImageUrl);

public sealed record UpdateProductRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(0.01, 1_000_000)] decimal Price,
    [Range(0, int.MaxValue)] int Stock,
    [Url] string? ImageUrl);

public sealed record ProductDto(Guid Id, string Name, string? Description, decimal Price, int Stock, string? ImageUrl)
{
    public static ProductDto From(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Stock, product.ImageUrl);
}
