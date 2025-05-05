using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace API_ASP.Models;

public partial class ASPBDContext : DbContext
{
    public ASPBDContext()
    {
    }

    public ASPBDContext(DbContextOptions<ASPBDContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Catalog> Catalogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<PosOrder> PosOrders { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-KED139H\\SQLEXPRESS;Database=ASPBD;Trusted_Connection=True;Encrypt=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Catalog>(entity =>
        {
            entity.HasKey(e => e.CatalogsId).HasName("PK__catalogs__5D5495DDD080DC99");

            entity.ToTable("catalogs");

            entity.Property(e => e.CatalogsId).HasColumnName("catalogsID");
            entity.Property(e => e.Author)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("author");
            entity.Property(e => e.Price)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("price");
            entity.Property(e => e.Publisher)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("publisher");
            entity.Property(e => e.Title)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.YearPublic).HasColumnName("year_public");

            entity.HasMany(d => d.Categories).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ProductCa__categ__59063A47"),
                    l => l.HasOne<Catalog>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ProductCa__produ__5812160E"),
                    j =>
                    {
                        j.HasKey("ProductId", "CategoryId").HasName("PK__ProductC__1A56936E1BB76379");
                        j.ToTable("ProductCategory");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__23CAF1F8637A5C75");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("categoryID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrdersId).HasName("PK__orders__5F11BFD501794949");

            entity.ToTable("orders");

            entity.Property(e => e.OrdersId).HasColumnName("ordersID");
            entity.Property(e => e.CatalogsId).HasColumnName("catalogs_id");
            entity.Property(e => e.TotalSum)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_sum");
            entity.Property(e => e.UsersId).HasColumnName("users_id");

            entity.HasOne(d => d.Catalogs).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CatalogsId)
                .HasConstraintName("FK__orders__catalogs__5070F446");

            entity.HasOne(d => d.Users).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UsersId)
                .HasConstraintName("FK__orders__users_id__4F7CD00D");
        });

        modelBuilder.Entity<PosOrder>(entity =>
        {
            entity.HasKey(e => e.PosOrderId).HasName("PK__PosOrder__72F3E628BE819CDD");

            entity.ToTable("PosOrder");

            entity.Property(e => e.PosOrderId).HasColumnName("pos_order_id");
            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Order).WithMany(p => p.PosOrders)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__PosOrder__order___534D60F1");

            entity.HasOne(d => d.Product).WithMany(p => p.PosOrders)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__PosOrder__produc__5441852A");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79AEC4D63C77");

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Reviews__Product__5CD6CB2B");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__UserID__5DCAEF64");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A817D5A33");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160D3C6CF4F").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__CB9A1CDF786DB3F0");

            entity.ToTable("users");

            entity.HasIndex(e => e.Login, "UQ__users__7838F272F60F112C").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__users__AB6E6164930A6919").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Email)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Login)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasMaxLength(64)
                .HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__users__RoleID__4AB81AF0");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
