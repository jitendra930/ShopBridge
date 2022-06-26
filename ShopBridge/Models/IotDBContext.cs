using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ShopBridge.Models
{
    public partial class IotDBContext : DbContext
    {
        public IotDBContext()
        {
        }

        public IotDBContext(DbContextOptions<IotDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ShopBridgeInventory> ShopBridgeInventories { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=tcp:iothubcoreading.database.windows.net,1433;Initial Catalog=IotDB;Persist Security Info=False;User ID=netzero;Password=Jitendra@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShopBridgeInventory>(entity =>
            {
                entity.ToTable("ShopBridgeInventory");

                entity.Property(e => e.Description)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
