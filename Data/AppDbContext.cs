using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using webshop.Models;


namespace webshop.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<SiteSettings> SiteSettings { get; set; }

    public DbSet<StockLog> StockLogs { get; set; }

    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponUsage> CouponUsages { get; set; }

    public DbSet<ProductReview> ProductReviews { get; set; }

    public DbSet<ProductImage> ProductImages { get; set; }

    public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }

    public DbSet<BlacklistedUser> BlacklistedUsers { get; set; }

    public DbSet<StockEntry> StockEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Kategória - Termék kapcsolat
        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Termék - Rendelési tétel kapcsolat (Védelem)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // 3. Termék - Galéria képek (Cascade törlés)
        builder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.GalleryImages)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // 4. Termék - Készletnapló kapcsolat (Védelem)
        // Ha a terméket törlik, a naplóbejegyzés maradjon meg, csak a ProductId legyen NULL
        builder.Entity<StockLog>()
            .HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // 5. Precízió az áraknak
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);

        // 6. Egyedi indexek
        builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
    }
}