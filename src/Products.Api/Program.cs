using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Products.Api.Filters;
using Products.Api.Middleware;
using Products.Application.Abstractions;
using Products.Application.Services;
using Products.Application.Validation;
using Products.Infrastructure;
using Products.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("ProductsDb")))
    throw new InvalidOperationException(
        "ConnectionStrings:ProductsDb is not configured. " +
        "For local development set it via User Secrets (see README > Local credentials via User Secrets); " +
        "in containers it is injected via the ConnectionStrings__ProductsDb environment variable.");

builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Apply migrations and seed initial data in Development only.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
    var cancellationToken = app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    await dbContext.Database.MigrateAsync(cancellationToken);
    await DbSeeder.SeedAsync(dbContext, app.Logger, timeProvider, cancellationToken);
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
