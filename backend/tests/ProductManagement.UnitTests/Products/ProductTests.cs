using ProductManagement.Domain.Common;
using ProductManagement.Domain.Models;

namespace ProductManagement.UnitTests.Products;

public class ProductTests
{
    private const int BaseTestProductStock = 10;
    private static Product CreateValidProduct()
    {
        return Product.Create(
            name: "16 GB RAM stick",
            price: 299,
            stock: BaseTestProductStock
        );
    }

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var name = "CPU";
        var price = 500m;
        var stock = 20;

        var p = Product.Create(name, price, stock);


        Assert.Equal(name, p.Name);
        Assert.Equal(price, p.Price);
        Assert.Equal(stock, p.Stock);
        Assert.Equal(DateTimeKind.Utc, p.CreatedAt.Kind);
        Assert.Equal(p.CreatedAt, p.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithMissingName_Throws(string? name)
    {
        // When the assertion is the same and the only thing that differs are inputs,
        // we can use [Theory] with [InlineData] to run 1 test per InlineData fed.
        // This avoids having to copy paste the same test for different inputs.
        var price = 500m;
        var stock = 20;

        Assert.Throws<DomainException>(() =>
        {
            Product.Create(name!, price, stock);
        });
    }

    [Theory]
    [InlineData("A")]
    [InlineData("The quick brown fox jumps over the lazy coding dog. This name exceeds the maximum allowed length for a product.")]
    public void Create_WithInvalidNameLength_Throws(string name)
    {
        var price = 500m;
        var stock = 20;

        Assert.Throws<DomainException>(() =>
        {
            Product.Create(name!, price, stock);
        });
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        Assert.Equal("GPU", Product.Create("  GPU  ", 1m, 1).Name);
    }


    [Theory]
    [InlineData(-0.01)]
    public void Create_WithNegativePrice_Throws(decimal price)
    {
        Assert.Throws<DomainException>(() => Product.Create("GPU", price, 1));
    }

    [Fact]
    public void Create_WithNegativeStock_Throws()
    {
        Assert.Throws<DomainException>(() => Product.Create("GPU", 1m, -1));
    }


    [Fact]
    public void Update_ChangesFieldsAndBumpsUpdatedAt()
    {
        var product = CreateValidProduct();
        var before = product.UpdatedAt;

        var newProductName = "Exotic keyboard";
        var newProductPrice = 50m;

        product.Update(newProductName, newProductPrice);

        Assert.Equal(newProductName, product.Name);
        Assert.Equal(newProductPrice, product.Price);
        Assert.True(product.UpdatedAt >= before);
        Assert.Equal(product.CreatedAt, product.CreatedAt);
    }

    [Fact]
    public void Update_DoesNotChangeStock()
    {
        var product = CreateValidProduct();
        product.Update("Something", 199m);
        Assert.Equal(BaseTestProductStock, product.Stock);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("A")]
    [InlineData("The quick brown fox jumps over the lazy coding dog. This name exceeds the maximum allowed length for a product.")]
    public void Update_WithInvalidName_Throws(string? name)
    {
        var product = CreateValidProduct();
        Assert.Throws<DomainException>(() => product.Update(name!, 199m));
    }

    [Theory]
    [InlineData(-1)]
    public void Update_WithInvalidPrice_Throws(decimal price)
    {
        var product = CreateValidProduct();
        Assert.Throws<DomainException>(() => product.Update("Ocarina", price!));
    }
}