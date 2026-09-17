using Microsoft.EntityFrameworkCore;
using StockLentes.Models;
namespace StockLentes.Data;
public class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options) {
 public DbSet<OpticalStore> OpticalStores => Set<OpticalStore>();
 public DbSet<StockRule> StockRules => Set<StockRule>();
 public DbSet<LensFamily> LensFamilies => Set<LensFamily>();
 public DbSet<LensProduct> Products => Set<LensProduct>();
 public DbSet<LensOrder> Orders => Set<LensOrder>();
 public DbSet<OrderLens> OrderLenses => Set<OrderLens>();
 public DbSet<StockBalance> Stock => Set<StockBalance>();
 public DbSet<StockMovement> Movements => Set<StockMovement>();
 protected override void OnModelCreating(ModelBuilder b) {
  b.Entity<LensFamily>().HasOne(x=>x.Rule).WithMany().HasForeignKey(x=>x.StockRuleId);
  b.Entity<LensProduct>().HasOne(x=>x.Family).WithMany().HasForeignKey(x=>x.LensFamilyId);
  b.Entity<LensProduct>().HasIndex(x=>x.Code).IsUnique();
  b.Entity<LensProduct>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<StockBalance>().HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.LensProductId);
  b.Entity<StockBalance>().HasIndex(x=>new{x.LensProductId,x.CombinationKey}).IsUnique();
  b.Entity<StockBalance>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<StockBalance>().ToTable(t=>t.HasCheckConstraint("CK_Stock_Nonnegative","QuantityHalfPairs >= 0"));
  b.Entity<LensOrder>().HasOne(x=>x.Store).WithMany().HasForeignKey(x=>x.OpticalStoreId);
  b.Entity<LensOrder>().HasIndex(x=>new{x.OpticalStoreId,x.ExternalNumber}).IsUnique();
  b.Entity<LensOrder>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<OrderLens>().HasOne(x=>x.Order).WithMany(x=>x.Lenses).HasForeignKey(x=>x.LensOrderId);
  b.Entity<OrderLens>().HasOne(x=>x.RequestedProduct).WithMany().HasForeignKey(x=>x.RequestedProductId);
  // Partial or repeated input is preserved; conflicting eye/pair identities block completion instead.
  b.Entity<OrderLens>().HasIndex(x=>new{x.LensOrderId,x.Eye,x.PairType,x.PairNumber});
  b.Entity<OrderLens>().Property(x=>x.Version).IsConcurrencyToken();
  b.Entity<StockMovement>().HasOne(x=>x.OrderLens).WithMany().HasForeignKey(x=>x.OrderLensId);
  b.Entity<StockMovement>().HasOne(x=>x.ActualProduct).WithMany().HasForeignKey(x=>x.ActualProductId);
  b.Entity<StockMovement>().HasOne(x=>x.Balance).WithMany().HasForeignKey(x=>x.StockBalanceId);
  b.Entity<StockMovement>().HasIndex(x=>x.OrderLensId).IsUnique();
  b.Entity<StockMovement>().HasIndex(x=>x.IdempotencyKey).IsUnique();
  foreach(var fk in b.Model.GetEntityTypes().SelectMany(t=>t.GetForeignKeys())) fk.DeleteBehavior=DeleteBehavior.Restrict;
 }
}
