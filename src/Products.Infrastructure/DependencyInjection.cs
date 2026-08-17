using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Abstractions;
using Products.Infrastructure.Persistence;
using Products.Infrastructure.Repositories;

namespace Products.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProductsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ProductsDb"),
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
