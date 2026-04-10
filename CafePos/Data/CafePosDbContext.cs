using CafePos.Models;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Data
{
    public class CafePosDbContext : DbContext
    {
        public CafePosDbContext(DbContextOptions<CafePosDbContext> options) : base(options)
        {
        }

        // Danh sách các bảng sẽ được tạo trong SQL Server
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<Topping> Toppings { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemTopping> OrderItemToppings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình cột tiền tệ (18 chữ số tối đa, 0 chữ số thập phân vì dùng VNĐ)
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<OrderItemTopping>().Property(oit => oit.Price).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<Product>().Property(p => p.BasePrice).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<ProductSize>().Property(ps => ps.ExtraPrice).HasColumnType("decimal(18,0)");
            modelBuilder.Entity<Topping>().Property(t => t.Price).HasColumnType("decimal(18,0)");
        }
    }
}
