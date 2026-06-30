using CafeMenu.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cafe> Cafes {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // تنظیم قیمت با دو رقم اعشار در دیتابیس
        modelBuilder.Entity<MenuItem>()           
            .HasOne(m => m.Cafe)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CafeId)
            .OnDelete(DeleteBehavior.Restrict);

             modelBuilder.Entity<MenuItem>().Property(m => m.Price)
            .HasColumnType("decimal(18,2)");
    }

}