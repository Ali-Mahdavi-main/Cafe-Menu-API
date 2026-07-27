// AppDbContextFactory.cs (in CafeMenu.Api.Data)

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using CafeMenu.Api.Services;   // for ICurrentCafeService
using Microsoft.AspNetCore.Http;

namespace CafeMenu.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Build configuration to read the connection string
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        // Design-time dummies – enough to satisfy the constructor
        var httpContextAccessor = new DummyHttpContextAccessor();
        var currentCafeService = new DummyCurrentCafeService();

        return new AppDbContext(optionsBuilder.Options, httpContextAccessor, currentCafeService);
    }
}

internal class DummyHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = null;
}

internal class DummyCurrentCafeService : ICurrentCafeService
{
    public int? CafeId => null;  // no filtering → all rows visible during design-time
}