using Microsoft.EntityFrameworkCore;
using CBAS.Web.Models;

namespace CBAS.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Offer> Offers { get; set; } = null!;
    public DbSet<OfferLot> OfferLots { get; set; } = null!;
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
    }
}
