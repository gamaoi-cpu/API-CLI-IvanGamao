using InventoryApiCliClient.Models;
using InventoryApiCliClient.Services;
using System.Globalization;
using System.Net;

namespace InventoryApiCliClient;

public class Program
{
	private const string BaseUrl = "https://localhost:7298";

	public static async Task Main(string[] args)
	{
		try
		{
			using ApiClient apiClient = new(ResolveBaseUrl(args));
			await RunMenuAsync(apiClient);
		}
		catch (UriFormatException)
		{
			DisplayError("The API base URL is invalid. Update the BaseUrl value in Program.cs.");
		}
		catch (Exception exception)
		{
			DisplayError($"The application encountered an unexpected error: {exception.Message}");
		}
	}

	private static string ResolveBaseUrl(string[] args)
	{
		string configuredUrl = args.FirstOrDefault(argument => !string.IsNullOrWhiteSpace(argument))
			?? Environment.GetEnvironmentVariable("INVENTORY_API_BASE_URL")
			?? BaseUrl;

		return configuredUrl.TrimEnd('/') + "/";
	}

	private static async Task RunMenuAsync(ApiClient apiClient)
	{
		bool exit = false;

		while (!exit)
		{
			DisplayMainMenu();
			string choice = Console.ReadLine()?.Trim() ?? string.Empty;

			try
			{
				switch (choice)
				{
					case "1":
						await ViewProductsAsync(apiClient);
						break;
					case "2":
						await AddProductAsync(apiClient);
						break;
					case "3":
						await UpdateProductAsync(apiClient);
						break;
					case "4":
						await DeleteProductAsync(apiClient);
						break;
					case "5":
						await ViewCategoryByIdAsync(apiClient);
						break;
					case "6":
						await ViewCategoriesAsync(apiClient);
						break;
					case "7":
						exit = true;
						Console.WriteLine("\nThank you for using Inventory API CLI Client!");
						Console.WriteLine("Goodbye!");
						break;
					default:
						Console.WriteLine("\nInvalid menu choice. Please enter a number from 1 to 7.");
						break;
				}
			}
			catch (ApiException exception)
			{
				DisplayApiError(exception);
			}
			catch (HttpRequestException)
			{
				Console.WriteLine("ERROR: Could not connect to the REST API.");
			}
			catch (TaskCanceledException)
			{
				Console.WriteLine("ERROR: The API request timed out.");
			}
			catch (InvalidOperationException exception)
			{
				DisplayError(exception.Message);
			}
			catch (Exception exception)
			{
				DisplayError($"Unexpected error: {exception.Message}");
			}

			if (!exit)
			{
				Console.WriteLine("\nPress Enter to return to the main menu.");
				Console.ReadLine();
			}
		}
	}

	private static async Task ViewProductsAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n# ==============================================");
		Console.WriteLine("PRODUCT LIST");
		Console.WriteLine("# ==============================================");
		List<Product> products = await apiClient.GetProductsAsync();

		if (products.Count == 0)
		{
			Console.WriteLine("No products found.");
			return;
		}

		foreach (Product product in products)
		{
			Console.WriteLine($"\n## ID : {product.Id}");
			Console.WriteLine($"Name : {product.Name}");
			Console.WriteLine($"Description : {product.Description}");
			Console.WriteLine($"Price : {product.Price.ToString("C2", CultureInfo.GetCultureInfo("en-PH"))}");
			Console.WriteLine($"Stock : {product.StockQuantity}");
			Console.WriteLine($"Category ID : {product.CategoryId}");
		}
	}

	private static async Task AddProductAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n========== ADD PRODUCT ==========");
		Product product = ReadProductDetails();
		await apiClient.AddProductAsync(product);
		Console.WriteLine("Product successfully added!");
	}

	private static async Task UpdateProductAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n========== UPDATE PRODUCT ==========");
		int id = ReadId("Enter Product ID: ");
		Product product = ReadProductDetails("New ");
		await apiClient.UpdateProductAsync(id, product);
		Console.WriteLine("Product successfully updated!");
	}

	private static async Task DeleteProductAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n========== DELETE PRODUCT ==========");
		int id = ReadId("Enter Product ID: ");
		string confirmation = ReadConfirmation("Are you sure you want to delete this product? (Y/N): ");

		if (confirmation == "N")
		{
			Console.WriteLine("Delete operation cancelled.");
			return;
		}

		await apiClient.DeleteProductAsync(id);
		Console.WriteLine("Product successfully deleted!");
	}

	private static async Task ViewCategoriesAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n# ==============================================");
		Console.WriteLine("CATEGORY LIST");
		Console.WriteLine("# ==============================================");
		List<Category> categories = await apiClient.GetCategoriesAsync();

		if (categories.Count == 0)
		{
			Console.WriteLine("No categories found.");
			return;
		}

		foreach (Category category in categories)
		{
			Console.WriteLine($"\n## ID : {category.Id}");
			Console.WriteLine($"Name : {category.Name}");
			Console.WriteLine($"Description : {category.Description}");
		}
	}

	private static async Task ViewCategoryByIdAsync(ApiClient apiClient)
	{
		Console.WriteLine("\n========== CATEGORY DETAILS ==========");
		int id = ReadId("Enter Category ID: ");
		Category category = await apiClient.GetCategoryByIdAsync(id);

		Console.WriteLine($"\nID: {category.Id}");
		Console.WriteLine($"Name: {category.Name}");
		Console.WriteLine($"Description: {category.Description}");
	}

	private static Product ReadProductDetails(string prefix = "")
	{
		string name = ReadRequiredText($"{prefix}Product name: ");
		string description = ReadRequiredText($"{prefix}Description: ");
		decimal price = ReadNonNegativeDecimal($"{prefix}Price: ");
		int stockQuantity = ReadNonNegativeInt($"{prefix}Stock quantity: ");
		int categoryId = ReadId($"{prefix}Category ID: ");

		return new Product
		{
			Name = name,
			Description = description,
			Price = price,
			StockQuantity = stockQuantity,
			CategoryId = categoryId
		};
	}

	private static int ReadId(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			if (int.TryParse(Console.ReadLine(), out int id) && id > 0)
			{
				return id;
			}

			Console.WriteLine("Invalid ID. Please enter a valid positive number.");
		}
	}

	private static string ReadRequiredText(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			string value = Console.ReadLine()?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}

			Console.WriteLine("This field cannot be empty.");
		}
	}

	private static decimal ReadNonNegativeDecimal(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			if (decimal.TryParse(Console.ReadLine(), out decimal value) && value >= 0)
			{
				return value;
			}

			Console.WriteLine("Invalid price. Please enter a valid non-negative amount.");
		}
	}

	private static int ReadNonNegativeInt(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			if (int.TryParse(Console.ReadLine(), out int value) && value >= 0)
			{
				return value;
			}

			Console.WriteLine("Stock quantity cannot be negative and must be a whole number.");
		}
	}

	private static string ReadConfirmation(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			string value = (Console.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
			if (value is "Y" or "N")
			{
				return value;
			}

			Console.WriteLine("Invalid choice. Please enter Y or N.");
		}
	}

	private static void DisplayMainMenu()
	{
		Console.WriteLine("\n========================================");
		Console.WriteLine("       INVENTORY API CLI CLIENT");
		Console.WriteLine("========================================");
		Console.WriteLine("1. View Products");
		Console.WriteLine("2. Add Product");
		Console.WriteLine("3. Update Product");
		Console.WriteLine("4. Delete Product");
		Console.WriteLine("5. View Category by ID");
		Console.WriteLine("6. View Categories");
		Console.WriteLine("7. Exit");
		Console.Write("\nEnter choice: ");
	}

	private static void DisplayApiError(ApiException exception)
	{
		if (exception.StatusCode == HttpStatusCode.NotFound)
		{
			Console.WriteLine(exception.ResourceName == "Category" ? "Category not found." : "Product not found.");
			return;
		}

		DisplayError($"API Error: {(int)exception.StatusCode}");
	}

	private static void DisplayError(string message)
	{
		Console.WriteLine("\n========================================");
		Console.WriteLine("ERROR");
		Console.WriteLine("========================================");
		Console.WriteLine(message);
	}
}
