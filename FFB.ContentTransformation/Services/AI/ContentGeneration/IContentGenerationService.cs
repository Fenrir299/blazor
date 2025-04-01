// Services/AI/ContentGeneration/IContentGenerationService.cs
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
        /// Saves generated content to the database
        /// </summary>
        Task<GeneratedContent> SaveGeneratedContentAsync(Document document, string content, ContentGenerationOptions options);
    }
}
