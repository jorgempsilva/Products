using FluentAssertions;
using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Dtos;
using Products.Application.Services;
using Products.Domain.Entities;
using Products.Domain.Exceptions;

namespace Products.UnitTests;

public class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductService _sut;

    private static readonly DateTime FixedUtcNow = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public ProductServiceTests()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(FixedUtcNow));
        _sut = new ProductService(_repository, timeProvider);
    }

    private static Product SampleProduct(int id = 100001, int stock = 10) => new()
    {
        Id = id,
        Name = "Sample",
        Description = "Sample description",
        Price = 9.99m,
        Stock = stock,
        CreatedAtUtc = FixedUtcNow
    };

    [Fact]
    public async Task GetAll_WhenProductsExist_ShouldReturnMappedProductsWithStock()
    {
        // Arrange
        _repository.GetAllAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { SampleProduct(stock: 42) }, 1));

        // Act
        var result = await _sut.GetAllAsync(1, 20);

        // Assert
        result.Items.Should().ContainSingle()
            .Which.Stock.Should().Be(42);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ShouldReturnProduct()
    {
        // Arrange
        _repository.GetByIdAsync(100001, Arg.Any<CancellationToken>())
            .Returns(SampleProduct());

        // Act
        var result = await _sut.GetByIdAsync(100001);

        // Assert
        result.Id.Should().Be(100001);
        result.Name.Should().Be("Sample");
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldThrowProductNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        // Act
        var act = () => _sut.GetByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ShouldTrimFieldsSetCreatedAtAndPersist()
    {
        // Arrange
        var request = new CreateProductRequest("  New Product  ", " desc ", 10m, 5);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Name.Should().Be("New Product");
        result.Description.Should().Be("desc");
        result.CreatedAtUtc.Should().Be(FixedUtcNow);
        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "New Product" && p.Stock == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenProductExists_ShouldUpdateFieldsAndSetUpdatedAt()
    {
        // Arrange
        _repository.GetByIdAsync(100001, Arg.Any<CancellationToken>()).Returns(SampleProduct());
        _repository.UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.UpdateAsync(100001, new UpdateProductRequest("Updated", null, 20m));

        // Assert
        result.Name.Should().Be("Updated");
        result.Price.Should().Be(20m);
        result.Stock.Should().Be(10);
        result.UpdatedAtUtc.Should().Be(FixedUtcNow);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Product>(p => p.Id == 100001 && p.Name == "Updated" && p.Price == 20m && p.Stock == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenProductDoesNotExist_ShouldThrowProductNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var act = () => _sut.UpdateAsync(1, new UpdateProductRequest("X", null, 1m));

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Delete_WhenProductDoesNotExist_ShouldThrowProductNotFound()
    {
        // Arrange
        _repository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _sut.DeleteAsync(1);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Delete_WhenProductExists_ShouldComplete()
    {
        // Arrange
        _repository.DeleteAsync(100001, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.DeleteAsync(100001);

        // Assert
        await act.Should().NotThrowAsync();
        await _repository.Received(1).DeleteAsync(100001, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AddToStock_WhenQuantityIsNotPositive_ShouldThrowInvalidStockOperation(int quantity)
    {
        // Act
        var act = () => _sut.AddToStockAsync(100001, quantity);

        // Assert
        await act.Should().ThrowAsync<InvalidStockOperationException>();
    }

    [Fact]
    public async Task AddToStock_WhenProductDoesNotExist_ShouldThrowProductNotFound()
    {
        // Arrange
        _repository.IncrementStockAsync(1, 5, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _sut.AddToStockAsync(1, 5);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task AddToStock_WhenProductExists_ShouldComplete()
    {
        // Arrange
        _repository.IncrementStockAsync(100001, 5, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.AddToStockAsync(100001, 5);

        // Assert
        await act.Should().NotThrowAsync();
        await _repository.Received(1).IncrementStockAsync(100001, 5, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DecrementStock_WhenQuantityIsNotPositive_ShouldThrowInvalidStockOperation(int quantity)
    {
        // Act
        var act = () => _sut.DecrementStockAsync(100001, quantity);

        // Assert
        await act.Should().ThrowAsync<InvalidStockOperationException>();
    }

    [Fact]
    public async Task DecrementStock_WhenProductDoesNotExist_ShouldThrowProductNotFound()
    {
        // Arrange
        _repository.DecrementStockAsync(1, 5, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _sut.DecrementStockAsync(1, 5);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task DecrementStock_WhenStockIsInsufficient_ShouldThrowInsufficientStock()
    {
        // Arrange
        _repository.DecrementStockAsync(100001, 999, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsAsync(100001, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.DecrementStockAsync(100001, 999);

        // Assert
        (await act.Should().ThrowAsync<InsufficientStockException>())
            .Which.RequestedQuantity.Should().Be(999);
    }

    [Fact]
    public async Task DecrementStock_WhenStockIsSufficient_ShouldComplete()
    {
        // Arrange
        _repository.DecrementStockAsync(100001, 5, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.DecrementStockAsync(100001, 5);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchByName_WhenTermHasWhitespace_ShouldTrimSearchTerm()
    {
        // Arrange
        _repository.SearchByNameAsync("mouse", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { SampleProduct() }, 1));

        // Act
        var result = await _sut.SearchByNameAsync("  mouse  ", 1, 20);

        // Assert
        result.Items.Should().HaveCount(1);
        await _repository.Received(1).SearchByNameAsync("mouse", 1, 20, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, -1)]
    [InlineData(10, 5)]
    public async Task GetByStockRange_WhenRangeIsInvalid_ShouldThrowInvalidStockOperation(int min, int max)
    {
        // Act
        var act = () => _sut.GetByStockRangeAsync(min, max, 1, 20);

        // Assert
        await act.Should().ThrowAsync<InvalidStockOperationException>();
    }

    [Fact]
    public async Task GetByStockRange_WhenRangeIsValid_ShouldReturnProducts()
    {
        // Arrange
        _repository.GetByStockRangeAsync(0, 50, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { SampleProduct(stock: 25) }, 1));

        // Act
        var result = await _sut.GetByStockRangeAsync(0, 50, 1, 20);

        // Assert
        result.Items.Should().ContainSingle().Which.Stock.Should().Be(25);
    }
}
