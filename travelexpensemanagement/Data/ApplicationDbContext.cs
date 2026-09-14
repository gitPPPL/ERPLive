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

        //public DbSet<Department> Departments { get; set; } = null!;
        //public DbSet<Employee> Employees { get; set; } = null!;

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    modelBuilder.Entity<Department>()
        //        .HasMany(d => d.Employees)
        //        .WithOne(e => e.Department)
        //        .HasForeignKey(e => e.DepartmentId)
        //        .OnDelete(DeleteBehavior.SetNull);
        //}
    }
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Passport? Passport { get; set; }
    }

    public class Passport
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int PersonId { get; set; }
        public Person Person { get; set; } = null!;
    }
}
