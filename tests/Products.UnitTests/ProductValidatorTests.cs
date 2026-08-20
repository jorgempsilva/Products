using FluentAssertions;
using Products.Application.Dtos;
using Products.Application.Validation;

namespace Products.UnitTests;

public class ProductValidatorTests
{
    private readonly CreateProductRequestValidator _createValidator = new();
    private readonly UpdateProductRequestValidator _updateValidator = new();
    private readonly SearchProductsRequestValidator _searchValidator = new();
    private readonly PaginationRequestValidator _paginationValidator = new();

    [Fact]
    public void Validate_WhenCreateRequestIsValid_ShouldPass()
    {
        // Arrange
        var request = new CreateProductRequest("Product", "Desc", 9.99m, 10);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WhenCreateNameIsMissing_ShouldFail(string? name)
    {
        // Arrange
        var request = new CreateProductRequest(name!, null, 9.99m, 10);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Name));
    }

    [Fact]
    public void Validate_WhenCreateNameIsTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest(new string('a', 201), null, 9.99m, 10);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenCreatePriceIsNotPositive_ShouldFail(double price)
    {
        // Arrange
        var request = new CreateProductRequest("Product", null, (decimal)price, 10);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Price));
    }

    [Fact]
    public void Validate_WhenCreateStockIsNegative_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest("Product", null, 9.99m, -1);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Stock));
    }

    [Fact]
    public void Validate_WhenCreateDescriptionIsTooLong_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest("Product", new string('d', 1001), 9.99m, 10);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenUpdateRequestIsValid_ShouldPass()
    {
        // Arrange
        var request = new UpdateProductRequest("Product", null, 1m, 0);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUpdateRequestIsInvalid_ShouldCollectAllErrors()
    {
        // Arrange
        var request = new UpdateProductRequest("", null, 0m, -5);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should()
            .Contain([
                nameof(UpdateProductRequest.Name),
                nameof(UpdateProductRequest.Price),
                nameof(UpdateProductRequest.Stock)
            ]);
    }

    [Fact]
    public void Validate_WhenSearchNameIsValid_ShouldPass()
    {
        // Arrange
        var request = new SearchProductsRequest("mouse");

        // Act
        var result = _searchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WhenSearchNameIsMissing_ShouldFail(string? name)
    {
        // Arrange
        var request = new SearchProductsRequest(name);

        // Act
        var result = _searchValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchProductsRequest.Name));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, 50)]
    public void Validate_WhenPaginationIsWithinBounds_ShouldPass(int page, int pageSize)
    {
        // Arrange
        var request = new PaginationRequest { Page = page, PageSize = pageSize };

        // Act
        var result = _paginationValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsLessThanOne_ShouldFail(int page)
    {
        // Arrange
        var request = new PaginationRequest { Page = page };

        // Act
        var result = _paginationValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PaginationRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validate_WhenPageSizeIsOutOfBounds_ShouldFail(int pageSize)
    {
        // Arrange
        var request = new PaginationRequest { PageSize = pageSize };

        // Act
        var result = _paginationValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PaginationRequest.PageSize));
    }
}
