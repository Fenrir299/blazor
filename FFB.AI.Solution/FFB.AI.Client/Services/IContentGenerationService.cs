// FFB.AI.Client/Services/IContentGenerationService.cs
using FFB.AI.Shared.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FFB.AI.Client.Services
{
    public interface IContentGenerationService
    {
        Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request);
        Task<GeneratedContentDTO> GetGeneratedContentByIdAsync(int id);
        Task<IEnumerable<GeneratedContentDTO>> GetGeneratedContentsForDocumentAsync(int documentId);
        Task<GeneratedContentDTO> UpdateGeneratedContentAsync(int id, string title, string content);
    }
}