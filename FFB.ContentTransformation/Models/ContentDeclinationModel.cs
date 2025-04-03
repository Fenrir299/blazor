// Models/ContentDeclinationModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FFB.ContentTransformation.Data.Entities;
using FFB.ContentTransformation.Services.AI.ContentGeneration;

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

        public List<int> SelectedDocumentIds { get; set; } = new List<int>();

        public Document? SelectedDocument => SelectedDocumentId.HasValue ?
            UploadedDocuments.Find(d => d.Id == SelectedDocumentId) : null;

        public List<Document> SelectedDocuments => SelectedDocumentIds.Count > 0 ?
            UploadedDocuments.Where(d => SelectedDocumentIds.Contains(d.Id)).ToList() :
            (SelectedDocumentId.HasValue ?
                UploadedDocuments.Where(d => d.Id == SelectedDocumentId).ToList() :
                new List<Document>());

        public bool IsProcessing { get; set; } = false;

        public string? ErrorMessage { get; set; }

        public MultiDocumentStrategy MultiDocStrategy { get; set; } = MultiDocumentStrategy.Combine;

        public bool UseChunking { get; set; } = true;
    }
}