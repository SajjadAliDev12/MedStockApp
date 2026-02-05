using System;
using System.Collections.Generic;
using MedStock.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Data.Context;

public partial class HospitalInventoryDbContext : DbContext
{
    public HospitalInventoryDbContext()
    {
    }

    public HospitalInventoryDbContext(DbContextOptions<HospitalInventoryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Batch> Batches { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemCategory> ItemCategories { get; set; }

    public virtual DbSet<Requisition> Requisitions { get; set; }

    public virtual DbSet<RequisitionDetail> RequisitionDetails { get; set; }

    public virtual DbSet<RequisitionFulfillmentLink> RequisitionFulfillmentLinks { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Stocktake> Stocktakes { get; set; }

    public virtual DbSet<StocktakeDetail> StocktakeDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TransactionDetail> TransactionDetails { get; set; }

    public virtual DbSet<TransactionReason> TransactionReasons { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "IX_AuditLogs_Entity");

            entity.HasIndex(e => e.OccurredAt, "IX_AuditLogs_OccurredAt");

            entity.HasIndex(e => new { e.UserId, e.OccurredAt }, "IX_AuditLogs_UserId_OccurredAt");

            entity.Property(e => e.ActionType).HasMaxLength(30);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(80);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.MachineName).HasMaxLength(80);
            entity.Property(e => e.OccurredAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Summary).HasMaxLength(300);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__Batches__5D55CE5879196F2B");

            entity.HasIndex(e => new { e.ItemId, e.BatchCode }, "UQ_Batches_Item_Batch").IsUnique();

            entity.Property(e => e.BatchCode).HasMaxLength(60);
            entity.Property(e => e.CurrentQty).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.InitialQty).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.LocationCode).HasMaxLength(50);
            entity.Property(e => e.ReceivedDate).HasDefaultValueSql("(CONVERT([date],sysutcdatetime()))");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Item).WithMany(p => p.Batches)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Batches_Items");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_Name").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.DepartmentCode, "UQ_Departments_Code").IsUnique();

            entity.HasIndex(e => e.DepartmentName, "UQ_Departments_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DepartmentCode).HasMaxLength(30);
            entity.Property(e => e.DepartmentName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(300);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__Items__727E838BA4C8D00F");

            entity.HasIndex(e => e.Sku, "UQ__Items__CA1ECF0DC69B87EF").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.MinStok).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(30);
        });

        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.CategoryId });

            entity.HasIndex(e => e.CategoryId, "IX_ItemCategories_CategoryId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Category).WithMany(p => p.ItemCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ItemCategories_Categories");

            entity.HasOne(d => d.Item).WithMany(p => p.ItemCategories)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK_ItemCategories_Items");
        });

        modelBuilder.Entity<Requisition>(entity =>
        {
            entity.HasIndex(e => new { e.DepartmentId, e.Status }, "IX_Requisitions_Department_Status");

            entity.HasIndex(e => new { e.Status, e.RequestDate }, "IX_Requisitions_Status_Date");

            entity.HasIndex(e => e.RequisitionNo, "UQ_Requisitions_No").IsUnique();

            entity.Property(e => e.DecisionDate).HasPrecision(0);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.RequestDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RequisitionNo).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.RequisitionApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_Requisitions_ApprovedBy");

            entity.HasOne(d => d.Department).WithMany(p => p.Requisitions)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requisitions_Departments");

            entity.HasOne(d => d.RequestedByUser).WithMany(p => p.RequisitionRequestedByUsers)
                .HasForeignKey(d => d.RequestedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requisitions_RequestedBy");
        });

        modelBuilder.Entity<RequisitionDetail>(entity =>
        {
            entity.ToTable("Requisition_Details");

            entity.HasIndex(e => e.ItemId, "IX_RequisitionDetails_ItemId");

            entity.HasIndex(e => e.RequisitionId, "IX_RequisitionDetails_RequisitionId");

            entity.HasIndex(e => new { e.RequisitionId, e.ItemId }, "UQ_RequisitionDetails_Requisition_Item").IsUnique();

            entity.Property(e => e.FulfilledQty).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.RequestedQty).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Item).WithMany(p => p.RequisitionDetails)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequisitionDetails_Items");

            entity.HasOne(d => d.Requisition).WithMany(p => p.RequisitionDetails)
                .HasForeignKey(d => d.RequisitionId)
                .HasConstraintName("FK_RequisitionDetails_Requisitions");
        });

        modelBuilder.Entity<RequisitionFulfillmentLink>(entity =>
        {
            entity.HasKey(e => e.LinkId);

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("trg_RFL_UpdateFulfilledQty");
                    tb.HasTrigger("trg_RFL_ValidateLink");
                });

            entity.HasIndex(e => e.RequisitionDetailId, "IX_RFL_RequisitionDetailId");

            entity.HasIndex(e => e.TransactionDetailId, "IX_RFL_TransactionDetailId");

            entity.HasIndex(e => new { e.RequisitionDetailId, e.TransactionDetailId }, "UQ_RFL_UniqueLink").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FulfilledQty).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.RequisitionDetail).WithMany(p => p.RequisitionFulfillmentLinks)
                .HasForeignKey(d => d.RequisitionDetailId)
                .HasConstraintName("FK_RFL_RequisitionDetails");

            entity.HasOne(d => d.TransactionDetail).WithMany(p => p.RequisitionFulfillmentLinks)
                .HasForeignKey(d => d.TransactionDetailId)
                .HasConstraintName("FK_RFL_TransactionDetails");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.RoleName).HasMaxLength(80);
        });

        modelBuilder.Entity<Stocktake>(entity =>
        {
            entity.HasKey(e => e.StocktakeId).HasName("PK__Stocktak__5874C46567285311");

            entity.HasIndex(e => e.StocktakeNo, "UQ_Stocktakes_No").IsUnique();

            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PostedAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            entity.Property(e => e.StocktakeDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.StocktakeNo).HasMaxLength(30);

            entity.HasOne(d => d.CancelledByUser).WithMany(p => p.StocktakeCancelledByUsers)
                .HasForeignKey(d => d.CancelledByUserId)
                .HasConstraintName("FK_Stocktakes_CancelledBy");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.StocktakeCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stocktakes_CreatedBy");

            entity.HasOne(d => d.PostedByUser).WithMany(p => p.StocktakePostedByUsers)
                .HasForeignKey(d => d.PostedByUserId)
                .HasConstraintName("FK_Stocktakes_PostedBy");
        });

        modelBuilder.Entity<StocktakeDetail>(entity =>
        {
            entity.HasKey(e => e.StocktakeDetailId).HasName("PK__Stocktak__36CFC5B7821053E6");

            entity.ToTable("Stocktake_Details");

            entity.HasIndex(e => new { e.StocktakeId, e.ItemId }, "UX_StocktakeDetails_Stocktake_Item").IsUnique();

            entity.Property(e => e.Difference)
                .HasComputedColumnSql("(isnull([PhysicalQty],(0))-[SystemQty])", false)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.PhysicalQty).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SystemQty).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Item).WithMany(p => p.StocktakeDetails)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeDetails_Item");

            entity.HasOne(d => d.Reason).WithMany(p => p.StocktakeDetails)
                .HasForeignKey(d => d.ReasonId)
                .HasConstraintName("FK_StocktakeDetails_Reason");

            entity.HasOne(d => d.Stocktake).WithMany(p => p.StocktakeDetails)
                .HasForeignKey(d => d.StocktakeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeDetails_Stocktake");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.SupplierName).HasMaxLength(150);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A6B4FAB79E6");

            entity.ToTable(tb => tb.HasTrigger("trg_Transactions_ValidateReasonScope"));

            entity.HasIndex(e => e.CreatedByUserId, "IX_Transactions_CreatedByUserId");

            entity.HasIndex(e => e.DepartmentId, "IX_Transactions_DepartmentId");

            entity.HasIndex(e => e.ReasonId, "IX_Transactions_ReasonId");

            entity.HasIndex(e => e.TransactionNo, "UQ__Transact__554342D9B8FFC3B9").IsUnique();

            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TransactionNo).HasMaxLength(30);
            entity.Property(e => e.TransactionType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Transactions_Users_CreatedBy");

            entity.HasOne(d => d.Department).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Transactions_Departments");

            entity.HasOne(d => d.Reason).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.ReasonId)
                .HasConstraintName("FK_Transactions_TransactionReasons");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Transactions_Suppliers");
        });

        modelBuilder.Entity<TransactionDetail>(entity =>
        {
            entity.HasKey(e => e.TransactionDetailId).HasName("PK__Transact__F2B27FC65A6914EA");

            entity.ToTable("Transaction_Details", tb => tb.HasTrigger("trg_TransactionDetails_StockLedger"));

            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.SupplierRef).HasMaxLength(100);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Batch).WithMany(p => p.TransactionDetails)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TD_Batch");

            entity.HasOne(d => d.Transaction).WithMany(p => p.TransactionDetails)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TD_Transaction");
        });

        modelBuilder.Entity<TransactionReason>(entity =>
        {
            entity.HasKey(e => e.ReasonId);

            entity.HasIndex(e => e.IsActive, "IX_TransactionReasons_IsActive");

            entity.HasIndex(e => e.ReasonCode, "UQ_TransactionReasons_Code").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ReasonCode).HasMaxLength(40);
            entity.Property(e => e.ReasonName).HasMaxLength(150);
            entity.Property(e => e.Scope)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_Users_IsActive");

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(64);
            entity.Property(e => e.PasswordSalt).HasMaxLength(32);
            entity.Property(e => e.Username).HasMaxLength(80);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasIndex(e => e.RoleId, "IX_UserRoles_RoleId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserRoles_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
