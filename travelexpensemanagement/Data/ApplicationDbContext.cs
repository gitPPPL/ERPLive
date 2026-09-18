using DocumentFormat.OpenXml.Bibliography;
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

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasOne(e => e.Department).WithMany(d => d.Employees).HasForeignKey(e => e.DepartmentId);
        }

    }
    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        public int DepartmentId { get; set; }

        public byte[] RowVersion { get; set; } = null!;

        public Department Department { get; set; } = null!;
    }
    public class Department
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Employee> Employees { get; set; } = new();
    }
}
