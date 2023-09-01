using Microsoft.EntityFrameworkCore;

using Blog.Authors.API.Models;

namespace Blog.Authors.API.Data
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Author> Authors { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>(builder =>
            {
                builder.ToTable("Authors");

                builder.HasKey(a => a.Id);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
