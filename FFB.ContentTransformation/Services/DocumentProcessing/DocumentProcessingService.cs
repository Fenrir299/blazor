// Services/DocumentProcessing/DocumentProcessingService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data;
using FFB.ContentTransformation.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FFB.ContentTransformation.Services.DocumentProcessing
{
    /// <summary>
    /// Service for processing uploaded documents
    /// </summary>
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly DocumentTextExtractor _textExtractor;

        public DocumentProcessingService(
            IWebHostEnvironment environment,
            AppDbContext dbContext,
            ILogger<DocumentProcessingService> logger,
            DocumentTextExtractor textExtractor)
        {
            _environment = environment;
            _dbContext = dbContext;
            _logger = logger;
            _textExtractor = textExtractor;
        }

        public async Task<Document> UploadDocumentAsync(IBrowserFile file)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate a unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save the file with a retry mechanism
                await SaveFileWithRetryAsync(file, filePath);

                // Create document record
                var document = new Document
                {
                    FileName = file.Name,
                    FileType = file.ContentType,
                    StoragePath = uniqueFileName,
                    FileSize = file.Size,
                    UploadedAt = DateTime.UtcNow,
                    ProcessingComplete = false
                };

                _dbContext.Documents.Add(document);
                await _dbContext.SaveChangesAsync();

                // Extract text asynchronously
                // Utiliser Task.Run pour éviter de bloquer le thread UI
                _ = Task.Run(async () => await ExtractAndUpdateTextAsync(document));

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName}", file.Name);
                throw;
            }
        }

        private async Task SaveFileWithRetryAsync(IBrowserFile file, string filePath, int maxRetries = 3)
        {
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    // Utiliser FileShare.None pour indiquer que nous avons besoin d'un accès exclusif
                    await using var fileStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);

                    await file.OpenReadStream(maxAllowedSize: 10485760) // 10MB max
                        .CopyToAsync(fileStream);

                    // S'assurer que le fileStream est correctement fermé
                    await fileStream.FlushAsync();
                    fileStream.Close();

                    success = true;
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to save file after {RetryCount} attempts", retryCount);
                        throw;
                    }

                    _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} saving file", retryCount, maxRetries);
                    await Task.Delay(100 * retryCount); // Backoff exponentiel
                }
            }
        }

        public async Task<string> ExtractTextFromDocumentAsync(Document document)
        {
            if (!string.IsNullOrEmpty(document.ExtractedText))
            {
                return document.ExtractedText;
            }

            return await _textExtractor.ExtractTextAsync(document);
        }

        private async Task ExtractAndUpdateTextAsync(Document document)
        {
            try
            {
                // Ajouter un délai pour s'assurer que le fichier est bien fermé avant d'essayer de l'extraire
                await Task.Delay(500);

                var text = await _textExtractor.ExtractTextAsync(document);

                document.ExtractedText = text;
                document.ProcessingComplete = true;

                _dbContext.Update(document);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document {DocumentId}", document.Id);
                document.ProcessingError = ex.Message;
                document.ProcessingComplete = true;

                _dbContext.Update(document);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            var document = await _dbContext.Documents.FindAsync(documentId);
            if (document == null)
            {
                return;
            }

            try
            {
                // Delete physical file
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.StoragePath);
                if (File.Exists(filePath))
                {
                    // Tentative de suppression avec une méthode robuste
                    await DeleteFileWithRetryAsync(filePath);
                }

                // Delete from database
                _dbContext.Documents.Remove(document);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                throw;
            }
        }

        private async Task DeleteFileWithRetryAsync(string filePath, int maxRetries = 3)
        {
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    File.Delete(filePath);
                    success = true;
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to delete file after {RetryCount} attempts", retryCount);
                        throw;
                    }

                    _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} deleting file", retryCount, maxRetries);
                    await Task.Delay(100 * retryCount); // Backoff exponentiel
                }
            }
        }
    }
}