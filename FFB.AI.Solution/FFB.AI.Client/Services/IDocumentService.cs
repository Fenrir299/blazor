// FFB.AI.Client/Services/IDocumentService.cs
using FFB.AI.Shared.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FFB.AI.Client.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDTO>> GetAllDocumentsAsync();
        Task<DocumentDTO> GetDocumentByIdAsync(int id);
        Task<DocumentDTO> UploadDocumentAsync(MultipartFormDataContent formData);
        Task<DocumentDTO> ProcessDocumentAsync(int id);
        Task DeleteDocumentAsync(int id);
    }
}