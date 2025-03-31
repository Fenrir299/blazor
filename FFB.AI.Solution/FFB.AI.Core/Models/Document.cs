// FFB.AI.Core/Models/Document.cs
using System;
using System.Collections.Generic;

namespace FFB.AI.Core.Models
{
    /// <summary>
    /// Représente un document source dans le système
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string BlobStorageUrl { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }

        // Navigation properties
        public List<DocumentSegment> Segments { get; set; } = new();
        public List<GeneratedContent> GeneratedContents { get; set; } = new();
    }

    /// <summary>
    /// Représente un segment extrait d'un document pour la recherche
    /// </summary>
    public class DocumentSegment
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int SegmentNumber { get; set; }
        public string VectorEmbedding { get; set; } = string.Empty; // Stockage de l'embedding en base64

        // Navigation properties
        public Document Document { get; set; }
    }

    /// <summary>
    /// Représente un contenu généré à partir d'un document source
    /// </summary>
    public class GeneratedContent
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty; // Article, Post LinkedIn, etc.
        public string Tone { get; set; } = string.Empty; // Formel, Informel, etc.
        public string TargetAudience { get; set; } = string.Empty; // Public cible
        public int WordCount { get; set; }
        public DateTime GenerationDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;

        // Navigation properties
        public Document SourceDocument { get; set; }
    }

    /// <summary>
    /// Représente une recherche effectuée par l'utilisateur
    /// </summary>
    public class SearchQuery
    {
        public int Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public DateTime QueryDate { get; set; }
        public string UserId { get; set; } = string.Empty;

        // Navigation properties
        public List<SearchResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Représente un résultat de recherche
    /// </summary>
    public class SearchResult
    {
        public int Id { get; set; }
        public int SearchQueryId { get; set; }
        public int DocumentId { get; set; }
        public int? DocumentSegmentId { get; set; }
        public string GeneratedAnswer { get; set; } = string.Empty;
        public double Relevance { get; set; }

        // Navigation properties
        public SearchQuery SearchQuery { get; set; }
        public Document Document { get; set; }
        public DocumentSegment? DocumentSegment { get; set; }
    }
}

// FFB.AI.Infrastructure/Data/FFBDbContext.cs
using FFB.AI.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FFB.AI.Infrastructure.Data
{
    public class FFBDbContext : DbContext
    {
        public FFBDbContext(DbContextOptions<FFBDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentSegment> DocumentSegments { get; set; }
        public DbSet<GeneratedContent> GeneratedContents { get; set; }
        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<SearchResult> SearchResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuration des relations
            modelBuilder.Entity<Document>()
                .HasMany(d => d.Segments)
                .WithOne(s => s.Document)
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasMany(d => d.GeneratedContents)
                .WithOne(g => g.SourceDocument)
                .HasForeignKey(g => g.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SearchQuery>()
                .HasMany(q => q.Results)
                .WithOne(r => r.SearchQuery)
                .HasForeignKey(r => r.SearchQueryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Autres configurations
            modelBuilder.Entity<Document>()
                .Property(d => d.UploadDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<GeneratedContent>()
                .Property(g => g.GenerationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<SearchQuery>()
                .Property(q => q.QueryDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}

// FFB.AI.Shared/DTO/ContentGenerationDTO.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace FFB.AI.Shared.DTO
{
    /// <summary>
    /// DTO pour la demande de génération de contenu
    /// </summary>
    public class ContentGenerationRequest
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public string ContentType { get; set; } = "Article"; // Article, Post LinkedIn, etc.

        [Required]
        public string Tone { get; set; } = "Formel"; // Formel, Informel, etc.

        [Required]
        public string TargetAudience { get; set; } = "Professionnel"; // Public cible

        [Range(100, 5000)]
        public int MaxWordCount { get; set; } = 500;

        public bool Simplify { get; set; } = true;

        public bool Summarize { get; set; } = false;
    }

    /// <summary>
    /// DTO pour la réponse de génération de contenu
    /// </summary>
    public class ContentGenerationResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int WordCount { get; set; }
        public DateTime GenerationDate { get; set; }
    }

    /// <summary>
    /// DTO pour la recherche dans le corpus documentaire
    /// </summary>
    public class DocumentSearchRequest
    {
        [Required]
        [MinLength(3)]
        public string Query { get; set; } = string.Empty;

        public int MaxResults { get; set; } = 5;
    }

    /// <summary>
    /// DTO pour la réponse de recherche
    /// </summary>
    public class DocumentSearchResponse
    {
        public int QueryId { get; set; }
        public string GeneratedAnswer { get; set; }
        public List<DocumentSearchResult> Sources { get; set; } = new();
    }

    /// <summary>
    /// DTO pour un résultat de recherche individuel
    /// </summary>
    public class DocumentSearchResult
    {
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public string SegmentContent { get; set; }
        public double Relevance { get; set; }
    }
}