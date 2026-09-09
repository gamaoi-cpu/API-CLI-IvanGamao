using InventoryApiCliClient.Services;
using Xunit;

namespace InventoryApiCliClient.Tests;

public class ApiClientFallbackTests
{
    [Fact]
    public async Task GetProductsAsync_ShouldReturnSeededData_WhenApiUnavailable()
    {
        using var client = new ApiClient("https://localhost:1");

        var products = await client.GetProductsAsync();

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == "Laptop");
    }
}
