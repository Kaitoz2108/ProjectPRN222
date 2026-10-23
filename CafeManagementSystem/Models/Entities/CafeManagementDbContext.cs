using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementSystem.Models.Entities;

public partial class CafeManagementDbContext : DbContext
{
    public CafeManagementDbContext()
    {
    }

    public CafeManagementDbContext(DbContextOptions<CafeManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<CashierShift> CashierShifts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductRecipe> ProductRecipes { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WorkSchedule> WorkSchedules { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=CafeManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261CD14EEFC5");

            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Present");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendances_Schedule");

            entity.HasOne(d => d.Staff).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendances_Staff");
        });

        modelBuilder.Entity<CashierShift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__CashierS__C0A838813EB969F8");

            entity.Property(e => e.ClosingActualCash).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Discrepancy)
                .HasComputedColumnSql("([ClosingActualCash]-([OpeningCash]+[SystemCashSales]))", false)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.HandoverNotes).HasMaxLength(500);
            entity.Property(e => e.OpeningCash).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShiftStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Open");
            entity.Property(e => e.StartTime).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SystemCashSales).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Cashier).WithMany(p => p.CashierShifts)
                .HasForeignKey(d => d.CashierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shifts_Cashier");

            entity.HasOne(d => d.PreviousShift).WithMany(p => p.InversePreviousShift)
                .HasForeignKey(d => d.PreviousShiftId)
                .HasConstraintName("FK_Shifts_PrevShift");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B67AB9FE3");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("PK__Complain__740D898FDB2FA40E");

            entity.Property(e => e.AdminResponse).HasMaxLength(1000);
            entity.Property(e => e.ComplaintStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ComplaintType).HasMaxLength(50);
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EvidenceImageUrl).HasMaxLength(500);
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.ComplaintCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Complaints_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Complaints_Orders");

            entity.HasOne(d => d.ResolvedByAdmin).WithMany(p => p.ComplaintResolvedByAdmins)
                .HasForeignKey(d => d.ResolvedByAdminId)
                .HasConstraintName("FK_Complaints_Admin");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__626D8FCED14E47DA");

            entity.HasIndex(e => e.OrderId, "UQ__Deliveri__C3905BCE6B682C11").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CodAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Assigned");
            entity.Property(e => e.ProblemNote).HasMaxLength(500);
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.RecipientPhone).HasMaxLength(20);
            entity.Property(e => e.ShippingAddress).HasMaxLength(300);

            entity.HasOne(d => d.DeliveryStaff).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.DeliveryStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deliveries_Staff");

            entity.HasOne(d => d.HandedCodToShift).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.HandedCodToShiftId)
                .HasConstraintName("FK_Deliveries_Shift");

            entity.HasOne(d => d.Order).WithOne(p => p.Delivery)
                .HasForeignKey<Delivery>(d => d.OrderId)
                .HasConstraintName("FK_Deliveries_Orders");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PK__Ingredie__BEAEB25A94288EF4");

            entity.Property(e => e.IngredientName).HasMaxLength(150);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MinStockLevel)
                .HasDefaultValue(100m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StockQuantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(30);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.InventoryTransId).HasName("PK__Inventor__1464F9627515D26E");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.QuantityChanged).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReasonOrNote).HasMaxLength(300);
            entity.Property(e => e.TransactionType).HasMaxLength(30);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTrans_Ingredients");

            entity.HasOne(d => d.PerformedByUser).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.PerformedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTrans_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF92D72DC4");

            entity.HasIndex(e => e.OrderCode, "UQ__Orders__999B52292FAEC2E4").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderCode).HasMaxLength(30);
            entity.Property(e => e.OrderNote).HasMaxLength(300);
            entity.Property(e => e.OrderSource).HasMaxLength(20);
            entity.Property(e => e.PreparationStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Barista).WithMany(p => p.OrderBarista)
                .HasForeignKey(d => d.BaristaId)
                .HasConstraintName("FK_Orders_Barista");

            entity.HasOne(d => d.Cashier).WithMany(p => p.OrderCashiers)
                .HasForeignKey(d => d.CashierId)
                .HasConstraintName("FK_Orders_Cashier");

            entity.HasOne(d => d.CashierShift).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CashierShiftId)
                .HasConstraintName("FK_Orders_Shift");

            entity.HasOne(d => d.Customer).WithMany(p => p.OrderCustomers)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Orders_Customer");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED068116535615");

            entity.Property(e => e.IceLevel)
                .HasMaxLength(30)
                .HasDefaultValue("100%");
            entity.Property(e => e.ItemNote).HasMaxLength(255);
            entity.Property(e => e.SugarLevel)
                .HasMaxLength(30)
                .HasDefaultValue("100%");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Variants");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__PaymentT__9B556A381DF3C2C6");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.GatewayResponseCode).HasMaxLength(50);
            entity.Property(e => e.GatewayTransactionRef).HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasMaxLength(30);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasDefaultValue("Payment");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CDC9D86FAD");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.ProductName).HasMaxLength(150);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<ProductRecipe>(entity =>
        {
            entity.HasKey(e => e.RecipeId).HasName("PK__ProductR__FDD988B02440CC57");

            entity.HasIndex(e => new { e.VariantId, e.IngredientId }, "UQ_Variant_Ingredient").IsUnique();

            entity.Property(e => e.PreparationGuide).HasMaxLength(500);
            entity.Property(e => e.QuantityRequired).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recipes_Ingredients");

            entity.HasOne(d => d.Variant).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK_Recipes_Variants");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("PK__ProductV__0EA2338478FA710C");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SizeName).HasMaxLength(20);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductVariants_Products");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AA056D7A2");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616075C3D663").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C4119EC94");

            entity.HasIndex(e => e.Phone, "UQ__Users__5C7E359E922831CF").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534647CBCA7").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__WorkSche__9C8A5B499B76CD4E");

            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ShiftName).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByAdmin).WithMany(p => p.WorkScheduleCreatedByAdmins)
                .HasForeignKey(d => d.CreatedByAdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkSchedules_Admin");

            entity.HasOne(d => d.Staff).WithMany(p => p.WorkScheduleStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkSchedules_Staff");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
