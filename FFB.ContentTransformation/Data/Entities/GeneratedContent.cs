// Data/Entities/GeneratedContent.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FFB.ContentTransformation.Data.Entities
{
    /// <summary>
    /// Represents generated content from source documents
    /// </summary>
    public class GeneratedContent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Document")]
        public int DocumentId { get; set; }

        public Document? Document { get; set; }

        [Required]
        public ContentType ContentType { get; set; }

        [Required]
        public ContentFormat Format { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? CustomPrompt { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }
}