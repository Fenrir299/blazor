// Models/ContentDeclinationModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Models
{
    /// <summary>
    /// Model for content declination page
    /// </summary>
    public class ContentDeclinationModel
    {
        public List<Document> UploadedDocuments { get; set; } = new List<Document>();

        [Required]
        public ContentType SelectedContentType { get; set; } = ContentType.WebArticle;

        [Required]
        public ContentFormat SelectedFormat { get; set; } = ContentFormat.Medium;

        public string? CustomPrompt { get; set; }

        public string? GeneratedContent { get; set; }

        public int? SelectedDocumentId { get; set; }

        public Document? SelectedDocument => SelectedDocumentId.HasValue ?
            UploadedDocuments.Find(d => d.Id == SelectedDocumentId) : null;

        public bool IsProcessing { get; set; } = false;

        public string? ErrorMessage { get; set; }
    }
}