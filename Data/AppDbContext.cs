using CafeMenu.Api.Models;
using CafeMenu.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentCafeService _currentCafeService;
    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor, ICurrentCafeService currentCafeService) 
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentCafeService = currentCafeService;
    }

    public DbSet<Cafe> Cafes { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }

    public int CurrentCafeId => int.TryParse(_httpContextAccessor.HttpContext?.Items["CafeId"]?.ToString(), out var id) ? id : 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<MenuItem>()
        .HasQueryFilter(
            c => _currentCafeService.CafeId == null
            || c.CafeId == _currentCafeService.CafeId
        );
    modelBuilder.Entity<MenuItem>()
        .HasOne(m => m.Cafe)
        .WithMany(c => c.MenuItems)
        .HasForeignKey(m => m.CafeId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<MenuItem>()
        .HasOne(m => m.Category)
        .WithMany(c => c.MenuItems)
        .HasForeignKey(m => m.CategoryId)
        .OnDelete(DeleteBehavior.NoAction);

    modelBuilder.Entity<MenuItem>()
        .Property(m => m.Price)
        .HasColumnType("decimal(18,2)");

    modelBuilder.Entity<Category>()
        .HasQueryFilter(
            c => _currentCafeService.CafeId == null
            || c.CafeId == _currentCafeService.CafeId
        );
    modelBuilder.Entity<Cafe>()
        .HasIndex(c => c.UserName)
        .IsUnique();
}
}