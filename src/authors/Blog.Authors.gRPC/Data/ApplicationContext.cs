using Microsoft.EntityFrameworkCore;

using Blog.Authors.gRPC.Models;

namespace Blog.Authors.gRPC.Data
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
                builder.HasKey(a => a.Id);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
