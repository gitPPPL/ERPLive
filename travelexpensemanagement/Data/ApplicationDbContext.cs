using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryDeliveryChallanMemoModel> InventoryDeliveryChallanMemos { get; set; } = null!;
        public DbSet<InventoryDeliveryChallanMemoModel> GateMemos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InventoryDeliveryChallanMemoModel>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<InventoryDeliveryChallanMemoModel>(entity =>
            {
                entity.ToTable("GATE_MEMO1");

                entity.HasNoKey();

                entity.Ignore(x => x.ACTION);
                entity.Ignore(x => x.items);
            });
        }
    }
}
