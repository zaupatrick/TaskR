using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace TaskR.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext()
        {
        }

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlServer(@"Data Source=.\SQLEXPRESS;Initial Catalog=TaskRR;integrated security = true; trustservercertificate = true");
            }
        }

        public virtual DbSet<AppUser> AppUsers { get; set; }
        public virtual DbSet<Todo> Todos { get; set; }
        public virtual DbSet<Aufgabe> Aufgaben { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
    }
}
