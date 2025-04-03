// Services/AI/ContentGeneration/IContentGenerationService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Services.AI.ContentGeneration
{
    /// <summary>
    /// Interface for content generation service
    /// </summary>
    public interface IContentGenerationService
    {
        /// <summary>
        /// Generates content based on source document and options
        /// </summary>
        Task<string> GenerateContentAsync(Document document, ContentGenerationOptions options);

        /// <summary>
        /// Generates content based on multiple source documents and options
        /// </summary>
        Task<string> GenerateContentAsync(IEnumerable<Document> documents, ContentGenerationOptions options);

        /// <summary>
        /// Generates content based on document ID(s) specified in options
        /// </summary>
        Task<string> GenerateContentFromDocumentIdsAsync(ContentGenerationOptions options);

        /// <summary>
        /// Saves generated content to the database
        /// </summary>
        Task<GeneratedContent> SaveGeneratedContentAsync(Document document, string content, ContentGenerationOptions options);

        /// <summary>
        /// Saves generated content to the database with multiple document references
        /// </summary>
        Task<GeneratedContent> SaveGeneratedContentAsync(IEnumerable<Document> documents, string content, ContentGenerationOptions options);
    }
}