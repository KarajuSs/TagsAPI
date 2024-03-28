using Microsoft.EntityFrameworkCore;
using TagsAPI.Model;

namespace TagsAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Tag> Tags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=tags.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>()
                .HasKey(t => t.Id); // Definicja klucza głównego
        }
    }
}
