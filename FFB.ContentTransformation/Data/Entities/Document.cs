// Data/Entities/Document.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace FFB.ContentTransformation.Data.Entities
{
    /// <summary>
    /// Represents a document uploaded to the system
    /// </summary>
    public partial class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public string StoragePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string? ExtractedText { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool ProcessingComplete { get; set; } = false;

        public string? ProcessingError { get; set; }
    }
}