using Microsoft.EntityFrameworkCore;
using CBAS.Web.Models;

namespace CBAS.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Shipper> Shippers { get; set; } = null!;
    public DbSet<Offer> Offers { get; set; } = null!;
    public DbSet<OfferLot> OfferLots { get; set; } = null!;
    public DbSet<Lot> Lots { get; set; } = null!;
    public DbSet<HVIReport> HVIReports { get; set; } = null!;
    public DbSet<ProcessedOutput> ProcessedOutputs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OfferLot>()
            .HasOne(ol => ol.Offer)
            .WithMany(o => o.OfferLots)
            .HasForeignKey(ol => ol.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessedOutput>()
            .HasOne(po => po.Offer)
            .WithMany()
            .HasForeignKey(po => po.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessedOutput>()
            .HasOne(po => po.OfferLot)
            .WithMany()
            .HasForeignKey(po => po.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HVIReport>()
            .HasIndex(h => h.LotCode)
            .IsUnique();

        modelBuilder.Entity<OfferLot>()
            .HasIndex(ol => ol.LotCode);

        modelBuilder.Entity<Shipper>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Lot>()
            .HasIndex(l => l.LotCode)
            .IsUnique();

        modelBuilder.Entity<Lot>()
            .HasOne(l => l.Shipper)
            .WithMany(s => s.Lots)
            .HasForeignKey(l => l.ShipperId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
