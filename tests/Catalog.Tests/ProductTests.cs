using Catalog.Api.Domain;
using Xunit;

namespace Catalog.Tests;

public class ProductTests
{
    [Fact]
    public void ReserveStock_decrements_stock_when_sufficient()
    {
        var product = Product.Create("Test Ürün", null, 10m, 5, null);

        product.ReserveStock(3);

        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void ReserveStock_throws_and_leaves_stock_unchanged_when_insufficient()
    {
        var product = Product.Create("Test Ürün", null, 10m, 2, null);

        Assert.Throws<InvalidOperationException>(() => product.ReserveStock(3));
        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void ReserveStock_throws_when_quantity_not_positive()
    {
        var product = Product.Create("Test Ürün", null, 10m, 5, null);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.ReserveStock(0));
    }

    [Fact]
    public void HasSufficientStock_matches_exact_and_exceeding_quantities()
    {
        var product = Product.Create("Test Ürün", null, 10m, 5, null);

        Assert.True(product.HasSufficientStock(5));
        Assert.False(product.HasSufficientStock(6));
    }
}
