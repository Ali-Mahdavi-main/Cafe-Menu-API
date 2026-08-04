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
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<CafeSubscription> CafeSubscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<CafeEvent> CafeEvents { get; set; }
    public DbSet<CafeDisableStatus> CafeDisableStatuses { get; set; }

    public int CurrentCafeId => int.TryParse(_httpContextAccessor.HttpContext?.Items["CafeId"]?.ToString(), out var id) ? id : 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MenuItem>()
            .HasQueryFilter(c => _currentCafeService.CafeId == null || c.CafeId == _currentCafeService.CafeId);

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
            .HasQueryFilter(c => _currentCafeService.CafeId == null || c.CafeId == _currentCafeService.CafeId);

        modelBuilder.Entity<Cafe>()
            .HasIndex(c => c.UserName)
            .IsUnique();

        modelBuilder.Entity<CafeSubscription>()
            .HasOne(s => s.Cafe)
            .WithMany()
            .HasForeignKey(s => s.CafeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CafeSubscription>()
            .HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Cafe)
            .WithMany()
            .HasForeignKey(p => p.CafeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Subscription)
            .WithMany()
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CafeEvent>()
            .HasOne(e => e.Cafe)
            .WithMany(c => c.CafeEvents)
            .HasForeignKey(e => e.CafeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}