using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Infrastructure.Interceptors;

namespace Pharmaceutical.Infrastructure;

public class PharmaceuticalDbContext : IdentityDbContext<AppUser>
{
    public PharmaceuticalDbContext(DbContextOptions<PharmaceuticalDbContext> options)
        : base(options)
    {
    }

    public DbSet<DrugCatalogEntity> Drugs { get; set; }
    public DbSet<SupplierEntity> Suppliers { get; set; }
    public DbSet<StockTransactionEntity> StockTransactions { get; set; }
    public DbSet<DrugBatchEntity> DrugBatches { get; set; }
    public DbSet<PurchaseOrderEntity> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLineEntity> PurchaseOrderLines { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DrugCatalogEntity>(entity =>
        {
            entity.ToTable("drugs");
            entity.HasKey(e => e.DrugId);
            entity.Property(e => e.DrugId).HasMaxLength(50);
            entity.Property(e => e.DrugName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TradeName).HasMaxLength(200);
            entity.Property(e => e.Specification).HasMaxLength(200);
            entity.Property(e => e.DosageForm).HasMaxLength(100);
            entity.Property(e => e.ApprovalNum).HasMaxLength(100);
            entity.Property(e => e.StorageCond).HasMaxLength(200);
            entity.Property(e => e.PurchasePrice).HasPrecision(10, 2);
            entity.Property(e => e.RetailPrice).HasPrecision(10, 2);
            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierEntity>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.SupplierId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<StockTransactionEntity>(entity =>
        {
            entity.ToTable("stock_transactions");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.TransactionId).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Drug)
                .WithMany()
                .HasForeignKey(e => e.DrugId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TransactionType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Operator).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);
        });

        modelBuilder.Entity<DrugBatchEntity>(entity =>
        {
            entity.ToTable("drug_batches");
            entity.HasKey(e => e.BatchId);
            entity.Property(e => e.BatchId).ValueGeneratedOnAdd();
            entity.Property(e => e.BatchNumber).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.Drug)
                .WithMany(d => d.Batches)
                .HasForeignKey(e => e.DrugId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DrugId, e.BatchNumber }).IsUnique();
        });

        modelBuilder.Entity<PurchaseOrderEntity>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderLineEntity>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(e => e.LineId);
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Drug)
                .WithMany()
                .HasForeignKey(e => e.DrugId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(100);
        });
    }
}
