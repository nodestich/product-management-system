using ProductManagement.Application.Mappers;
using ProductManagement.Domain.Models;

namespace ProductManagement.UnitTests.Mappers;

public class ProductMapperTests
{
    private static Product CreateProduct()
    {
        var product = Product.Create("Mechanical keyboard", 189.99m, 8);
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, 100012);
        return product;
    }

    [Fact]
    public void MapToDto_CopiesEveryField()
    {
        var product = CreateProduct();

        var dto = product.MapToDto();

        Assert.Equal(100012, dto.Id);
        Assert.Equal("Mechanical keyboard", dto.Name);
        Assert.Equal(189.99m, dto.Price);
        Assert.Equal(8, dto.Stock);
        Assert.Equal(product.CreatedAt, dto.CreatedAt);
        Assert.Equal(product.UpdatedAt, dto.UpdatedAt);
    }

    [Fact]
    public void MapToDto_ReturnsNewInstanceEachCall()
    {
        var product = CreateProduct();

        var first = product.MapToDto();
        var second = product.MapToDto();

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
    }

    [Fact]
    public void MapToDtos_MapsEveryProductInOrder()
    {
        var first = Product.Create("Monitor", 249.99m, 12);
        var second = Product.Create("Mouse pad", 12.00m, 3);

        var dtos = new[] { first, second }.MapToDtos();

        Assert.Equal(2, dtos.Count);
        Assert.Equal("Monitor", dtos[0].Name);
        Assert.Equal("Mouse pad", dtos[1].Name);
    }

    [Fact]
    public void MapToDtos_OnEmptySequence_ReturnsEmpty()
    {
        var dtos = Array.Empty<Product>().MapToDtos();

        Assert.Empty(dtos);
    }
}