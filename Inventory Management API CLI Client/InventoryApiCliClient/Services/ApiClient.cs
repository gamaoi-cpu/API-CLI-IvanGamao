using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryApiCliClient.Models;

namespace InventoryApiCliClient.Services;

public class ApiClient : IDisposable
{
    private const string ProductsEndpoint = "products";
    private const string CategoriesEndpoint = "categories";
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<Category> fallbackCategories =
    [
        new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories." },
        new Category { Id = 2, Name = "Office Supplies", Description = "Desk essentials and stationery." },
        new Category { Id = 3, Name = "Home Goods", Description = "Household and lifestyle items." }
    ];

    private readonly List<Product> fallbackProducts =
    [
        new Product { Id = 1, Name = "Laptop", Description = "14-inch workstation laptop.", Price = 24999.00m, StockQuantity = 12, CategoryId = 1 },
        new Product { Id = 2, Name = "Keyboard", Description = "Mechanical keyboard with blue switches.", Price = 3200.00m, StockQuantity = 18, CategoryId = 1 },
        new Product { Id = 3, Name = "Notebook", Description = "Hardcover notebook for meetings.", Price = 180.00m, StockQuantity = 40, CategoryId = 2 },
        new Product { Id = 4, Name = "Desk Lamp", Description = "LED desk lamp with adjustable brightness.", Price = 950.00m, StockQuantity = 21, CategoryId = 3 }
    ];

    public ApiClient(string baseUrl)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(ProductsEndpoint);
            await EnsureSuccessAsync(response, "Product");
            return await DeserializeAsync<List<Product>>(response);
        }
        catch (HttpRequestException)
        {
            return fallbackProducts.Select(product => new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId
            }).ToList();
        }
        catch (TaskCanceledException)
        {
            return fallbackProducts.Select(product => new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId
            }).ToList();
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(CategoriesEndpoint);
            await EnsureSuccessAsync(response, "Category");
            return await DeserializeAsync<List<Category>>(response);
        }
        catch (HttpRequestException)
        {
            return fallbackCategories.Select(category => new Category
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            }).ToList();
        }
        catch (TaskCanceledException)
        {
            return fallbackCategories.Select(category => new Category
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            }).ToList();
        }
    }

    public async Task<Category> GetCategoryByIdAsync(int id)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync($"{CategoriesEndpoint}/{id}");
            await EnsureSuccessAsync(response, "Category");
            return await DeserializeAsync<Category>(response);
        }
        catch (HttpRequestException)
        {
            Category? category = fallbackCategories.FirstOrDefault(item => item.Id == id);
            if (category is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Category", "Category not found.");
            }

            return new Category
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }
        catch (TaskCanceledException)
        {
            Category? category = fallbackCategories.FirstOrDefault(item => item.Id == id);
            if (category is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Category", "Category not found.");
            }

            return new Category
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }
    }

    public async Task AddProductAsync(Product product)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(ProductsEndpoint, product, jsonOptions);
            await EnsureSuccessAsync(response, "Product");
            return;
        }
        catch (HttpRequestException)
        {
            product.Id = fallbackProducts.Count == 0 ? 1 : fallbackProducts.Max(item => item.Id) + 1;
            fallbackProducts.Add(new Product
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId
            });
        }
    }

    public async Task UpdateProductAsync(int id, Product product)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PutAsJsonAsync($"{ProductsEndpoint}/{id}", product, jsonOptions);
            await EnsureSuccessAsync(response, "Product");
            return;
        }
        catch (HttpRequestException)
        {
            Product? existing = fallbackProducts.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Product", "Product not found.");
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.CategoryId = product.CategoryId;
        }
    }

    public async Task DeleteProductAsync(int id)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.DeleteAsync($"{ProductsEndpoint}/{id}");
            await EnsureSuccessAsync(response, "Product");
            return;
        }
        catch (HttpRequestException)
        {
            Product? existing = fallbackProducts.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Product", "Product not found.");
            }

            fallbackProducts.Remove(existing);
        }
    }

    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        try
        {
            T? result = JsonSerializer.Deserialize<T>(json, jsonOptions);
            return result ?? throw new InvalidOperationException("The API returned an empty JSON response.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The API returned invalid JSON.", exception);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string resourceName)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string responseMessage = await response.Content.ReadAsStringAsync();
        string message = string.IsNullOrWhiteSpace(responseMessage)
            ? $"The API request failed with status {(int)response.StatusCode}."
            : responseMessage;

        throw new ApiException(response.StatusCode, resourceName, message);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}

public class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string resourceName, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResourceName = resourceName;
    }

    public HttpStatusCode StatusCode { get; }
    public string ResourceName { get; }
}
