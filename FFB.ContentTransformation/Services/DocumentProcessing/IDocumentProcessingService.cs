// Services/DocumentProcessing/IDocumentProcessingService.cs
using System.IO;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace FFB.ContentTransformation.Services.DocumentProcessing
{
    /// <summary>
    /// Interface for document processing service
    /// </summary>
    public interface IDocumentProcessingService
    {
        /// <summary>
        /// Uploads a file to storage and creates a document entry
        /// </summary>
        Task<Document> UploadDocumentAsync(IBrowserFile file);

        /// <summary>
        /// Processes a document to extract text content
        /// </summary>
        Task<string> ExtractTextFromDocumentAsync(Document document);

        /// <summary>
        /// Deletes a document from storage and the database
        /// </summary>
        Task DeleteDocumentAsync(int documentId);
    }
}