using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Infrastructure.Persistence;

namespace Products.IntegrationTests;

public sealed class ProductsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"ProductsDb_Tests_{Guid.NewGuid():N}";

    private string ConnectionString
    {
        get
        {
            var baseConnection = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");

            if (string.IsNullOrWhiteSpace(baseConnection))
                return $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            var builder = new SqlConnectionStringBuilder(baseConnection)
            {
                InitialCatalog = _databaseName
            };

            return builder.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ProductsDb", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }
}
