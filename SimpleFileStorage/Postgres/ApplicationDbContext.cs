using Microsoft.EntityFrameworkCore;
using SimpleFileStorage.Model;

namespace SimpleFileStorage.Postgres
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<FileMetadata> Metadatas { get; set; }
        public DbSet<FileData> Files { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // настройка сущности FileMetadata
            modelBuilder.Entity<FileMetadata>(entity =>
            {
                // FileID как первичный ключ без автогенерации (EF fluent API)
                entity.HasKey(e => e.FileID);
                entity.Property(e => e.FileID).ValueGeneratedNever(); // отключить автозаполнение
            });

            // настройка сущности FileData
            modelBuilder.Entity<FileData>(entity =>
            {
                // FileID как первичный ключ без автогенерации (EF fluent API)
                entity.HasKey(e => e.FileID);
                entity.Property(e => e.FileID).ValueGeneratedNever(); // отключить автозаполнение
            });
        }
    }
}
