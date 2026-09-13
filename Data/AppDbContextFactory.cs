using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Http;

namespace CafeMenu.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        // Fail clearly if it wasn't loaded
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found."
            );
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        // Design-time dummy services
        var httpContextAccessor = new DummyHttpContextAccessor();
        var currentCafeService = new DummyCurrentCafeService();

        return new AppDbContext(
            optionsBuilder.Options,
            httpContextAccessor,
            currentCafeService
        );
    }
}

internal class DummyHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}

internal class DummyCurrentCafeService : ICurrentCafeService
{
    public int? CafeId => null;
}