using ManufacturingScheduler.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingScheduler.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ProductionOrder> Orders { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<ProductionSchedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductionOrder>()
                .HasMany(p => p.RequiredMaterials)
                .WithOne()
                .HasForeignKey("ProductionOrderId");

            modelBuilder.Entity<Machine>()
                .HasMany(m => m.ProductCapabilities)
                .WithOne()
                .HasForeignKey("MachineId");

            modelBuilder.Entity<Machine>()
                .HasMany(m => m.ScheduledMaintenance)
                .WithOne()
                .HasForeignKey("MachineId");
        }
    }
}
