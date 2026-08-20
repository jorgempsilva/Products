using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Products.Application.Dtos;

namespace Products.IntegrationTests;

public class ProductsEndpointsTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ProductResponse> CreateProductAsync(string name = "Test Product", decimal price = 10m, int stock = 20)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest(name, "Integration test product", price, stock));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    [Fact]
    public async Task GetAll_WhenProductsExist_ShouldReturnOkWithStockIncluded()
    {
        // Arrange
        var uniqueName = $"GetAll Product {Guid.NewGuid():N}";
        var created = await CreateProductAsync(uniqueName, stock: 20);

        // Act
        var products = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products");

        // Assert
        products.Should().NotBeNull();
        products!.Items.Should().ContainSingle(p => p.Id == created.Id)
            .Which.Stock.Should().Be(20);
    }

    [Fact]
    public async Task GetAll_WhenPageSizeIsProvided_ShouldReturnPagedMetadataAndRespectPageSize()
    {
        // Arrange
        var token = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++)
            await CreateProductAsync($"Paged {token} {i}", stock: 5);

        // Act
        var page = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products?page=1&pageSize=2");

        // Assert
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        page.TotalPages.Should().Be((int)Math.Ceiling(page.TotalCount / 2.0));
    }

    [Fact]
    public async Task GetAll_WhenPageSizeExceedsMax_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/products?pageSize=51");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WhenPageIsLessThanOne_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/products?page=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    public async Task Create_WhenRequestIsValid_ShouldReturnCreatedWithSixDigitIdAndLocationHeader()
    {
        // Arrange
        var request = new CreateProductRequest("Created Product", null, 15.50m, 5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Id.Should().BeInRange(100000, 999999);
        product.Name.Should().Be("Created Product");
    }

    [Fact]
    public async Task Create_WhenCalledMultipleTimes_ShouldGenerateUniqueIds()
    {
        // Act
        var first = await CreateProductAsync("Unique A");
        var second = await CreateProductAsync("Unique B");

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task Create_WhenBodyIsInvalid_ShouldReturnBadRequestWithValidationDetails()
    {
        // Arrange
        var request = new CreateProductRequest("", null, -1m, -5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(["Name", "Price", "Stock"]);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ShouldReturnProduct()
    {
        // Arrange
        var created = await CreateProductAsync("Get Me");

        // Act
        var product = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}");

        // Assert
        product!.Id.Should().Be(created.Id);
        product.Name.Should().Be("Get Me");
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldReturnNotFoundProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/products/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task Update_WhenProductExists_ShouldReturnUpdatedProduct()
    {
        // Arrange
        var created = await CreateProductAsync("Before Update");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest("After Update", "new desc", 99.99m, 7));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Name.Should().Be("After Update");
        updated.Price.Should().Be(99.99m);
        updated.Stock.Should().Be(7);
        updated.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PutAsJsonAsync("/api/products/999999",
            new UpdateProductRequest("X", null, 1m, 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Update_WhenBodyIsInvalid_ShouldReturnBadRequestWithValidationDetails()
    {
        // Arrange
        var created = await CreateProductAsync("Valid Before Invalid Update");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest("", null, -1m, -5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(["Name", "Price", "Stock"]);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldReturnNoContentAndRemoveProduct()
    {
        // Arrange
        var created = await CreateProductAsync("Delete Me");

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/products/{created.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/products/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DecrementStock_WhenStockIsSufficient_ShouldReturnOkAndUpdateStock()
    {
        // Arrange
        var created = await CreateProductAsync(stock: 10);

        // Act
        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/4", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}");
        product!.Stock.Should().Be(6);
    }

    [Fact]
    public async Task DecrementStock_WhenStockIsInsufficient_ShouldReturnUnprocessableEntityProblemDetails()
    {
        // Arrange
        var created = await CreateProductAsync(stock: 3);

        // Act
        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/10", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Insufficient stock");
    }

    [Fact]
    public async Task DecrementStock_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/products/999999/decrement-stock/1", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DecrementStock_WhenQuantityIsZero_ShouldReturnBadRequest()
    {
        // Arrange
        var created = await CreateProductAsync();

        // Act
        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/0", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddToStock_WhenProductExists_ShouldReturnOkAndUpdateStock()
    {
        // Arrange
        var created = await CreateProductAsync(stock: 5);

        // Act
        var response = await _client.PostAsync($"/api/products/{created.Id}/add-to-stock/15", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}");
        product!.Stock.Should().Be(20);
    }

    [Fact]
    public async Task AddToStock_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/products/999999/add-to-stock/5", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WhenNameMatchesPartially_ShouldReturnMatchingProducts()
    {
        // Arrange
        var uniqueToken = Guid.NewGuid().ToString("N");
        var uniqueName = $"SuperWidget {uniqueToken}";
        await CreateProductAsync(uniqueName);

        // Act
        var results = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>($"/api/products/search?name={uniqueToken}");

        // Assert
        results.Should().NotBeNull();
        results!.Items.Should().ContainSingle(p => p.Name == uniqueName);
    }

    [Fact]
    public async Task Search_WhenNameIsMissing_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/products/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WhenNoProductsMatch_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var nonExistentToken = $"NoMatch_{Guid.NewGuid():N}";

        // Act
        var results = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/products/search?name={nonExistentToken}");

        // Assert
        results.Should().NotBeNull();
        results!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhenNameIsLikeWildcard_ShouldTreatItAsLiteralNotMatchAll()
    {
        // Arrange
        var uniqueName = $"WildcardProbe {Guid.NewGuid():N}";
        await CreateProductAsync(uniqueName);

        // Act
        var results = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products/search?name=%25");

        // Assert
        results.Should().NotBeNull();
        results!.Items.Should().NotContain(p => p.Name == uniqueName);
    }

    [Fact]
    public async Task StockLevel_WhenRangeIsValid_ShouldReturnOnlyProductsWithinRange()
    {
        // Arrange
        var lowStockName = $"Low Stock Item {Guid.NewGuid():N}";
        var highStockName = $"High Stock Item {Guid.NewGuid():N}";
        await CreateProductAsync(lowStockName, stock: 2);
        await CreateProductAsync(highStockName, stock: 500);

        // Act
        var results = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products/stock-level?min=0&max=5");

        // Assert
        results.Should().NotBeNull();
        results!.Items.Should().ContainSingle(p => p.Name == lowStockName)
            .Which.Stock.Should().Be(2);
        results.Items.Should().NotContain(p => p.Name == highStockName);
        results.Items.Should().OnlyContain(p => p.Stock >= 0 && p.Stock <= 5);
    }

    [Fact]
    public async Task StockLevel_WhenRangeIsInvalid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/products/stock-level?min=10&max=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DecrementStock_WhenRequestsAreConcurrent_ShouldNeverOversell()
    {
        // Arrange
        var created = await CreateProductAsync("Concurrent Item", stock: 10);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _client.PostAsync($"/api/products/{created.Id}/decrement-stock/1", null));
        var responses = await Task.WhenAll(tasks);

        // Assert
        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rejected = responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        succeeded.Should().Be(10);
        rejected.Should().Be(10);

        var product = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}");
        product!.Stock.Should().Be(0);
    }
}
