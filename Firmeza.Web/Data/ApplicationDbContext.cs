using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
        
    }
    public DbSet<Models.Product> Products { get; set; }
    public DbSet<Models.Customer> Customers { get; set; }
    public DbSet<Models.Sale> Sales { get; set; }
    public DbSet<Models.SaleItem> SaleItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Models.Sale>().Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Entity<Models.Sale>().Property(s => s.Tax).HasPrecision(18, 2);
        builder.Entity<Models.Sale>().Property(s => s.Total).HasPrecision(18, 2);
        builder.Entity<Models.SaleItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Entity<Models.Sale>().HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Models.SaleItem>().HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
