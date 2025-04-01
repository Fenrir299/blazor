// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Data
{
    /// <summary>
    /// Main database context for the application
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<GeneratedContent> GeneratedContents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Document entity
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.FileName);

            // Configure GeneratedContent entity
            modelBuilder.Entity<GeneratedContent>()
                .HasOne(g => g.Document)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}