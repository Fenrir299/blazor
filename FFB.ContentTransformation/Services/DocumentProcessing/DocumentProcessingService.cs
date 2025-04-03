// Services/DocumentProcessing/DocumentProcessingService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data;
using FFB.ContentTransformation.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DocumentProcessingService(
            IWebHostEnvironment environment,
            AppDbContext dbContext,
            ILogger<DocumentProcessingService> logger,
            DocumentTextExtractor textExtractor,
            IServiceScopeFactory serviceScopeFactory)
        {
            _environment = environment;
            _dbContext = dbContext;
            _logger = logger;
            _textExtractor = textExtractor;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Document> UploadDocumentAsync(IBrowserFile file)
        {
            try
            {
                _logger.LogInformation("Starting upload of document: {FileName}", file.Name);

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created uploads directory at: {Path}", uploadsPath);
                }

                // Generate a unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);
                _logger.LogInformation("Generated unique file path: {FilePath}", filePath);

                // Save the file with a retry mechanism
                await SaveFileWithRetryAsync(file, filePath);
                _logger.LogInformation("File saved successfully: {FilePath}", filePath);

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
                _logger.LogInformation("Document record created with ID: {DocumentId}", document.Id);

                // Démarrer le traitement de manière asynchrone, mais ne pas attendre sa fin (fire-and-forget)
                // Capture the documentId to avoid capturing the whole document object
                int documentId = document.Id;
                _ = Task.Run(async () => await ProcessDocumentAsync(documentId));

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document {FileName}", file.Name);
                throw;
            }
        }

        /// <summary>
        /// Process document asynchronously with better error handling
        /// </summary>
        private async Task ProcessDocumentAsync(int documentId)
        {
            try
            {
                _logger.LogInformation("Starting async processing of document: {DocumentId}", documentId);

                // Récupérer une nouvelle instance du document à partir de la base de données
                Document? document;
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    document = await dbContext.Documents.FindAsync(documentId);

                    if (document == null)
                    {
                        _logger.LogWarning("Document {DocumentId} not found for processing", documentId);
                        return;
                    }
                }

                await ExtractAndUpdateTextAsync(document);
                _logger.LogInformation("Completed processing of document: {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during async processing of document {DocumentId}", documentId);

                // Mettre à jour le document avec l'erreur
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var dbDocument = await dbContext.Documents.FindAsync(documentId);
                    if (dbDocument != null)
                    {
                        dbDocument.ProcessingError = $"Erreur de traitement: {ex.Message}";
                        dbDocument.ProcessingComplete = true;

                        dbContext.Update(dbDocument);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Updated document {DocumentId} with error status", documentId);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update document {DocumentId} with error status", documentId);
                }
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
                    _logger.LogInformation("Attempt {RetryCount} to save file: {FilePath}", retryCount + 1, filePath);

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
                    _logger.LogInformation("File saved successfully: {FilePath}", filePath);
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to save file after {RetryCount} attempts: {FilePath}", retryCount, filePath);
                        throw;
                    }

                    _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} saving file: {FilePath}", retryCount, maxRetries, filePath);
                    await Task.Delay(100 * retryCount); // Backoff exponentiel
                }
            }
        }

        public async Task<string> ExtractTextFromDocumentAsync(Document document)
        {
            _logger.LogInformation("Extracting text from document {DocumentId}", document.Id);

            // Vérifier d'abord si le texte est déjà extrait
            if (!string.IsNullOrEmpty(document.ExtractedText))
            {
                _logger.LogInformation("Using cached extracted text for document {DocumentId}", document.Id);
                return document.ExtractedText;
            }

            // Si non, vérifier si le document est en base de données avec du texte extrait
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dbDocument = await dbContext.Documents.FindAsync(document.Id);

                if (dbDocument != null && !string.IsNullOrEmpty(dbDocument.ExtractedText))
                {
                    _logger.LogInformation("Found extracted text in database for document {DocumentId}", document.Id);
                    // Mettre à jour l'objet passé en paramètre
                    document.ExtractedText = dbDocument.ExtractedText;
                    document.ProcessingComplete = dbDocument.ProcessingComplete;
                    return dbDocument.ExtractedText;
                }
            }

            // Si toujours pas de texte, alors extraire
            _logger.LogInformation("Performing live extraction for document {DocumentId}", document.Id);
            return await _textExtractor.ExtractTextAsync(document) ?? string.Empty;
        }

        private async Task ExtractAndUpdateTextAsync(Document document)
        {
            try
            {
                _logger.LogInformation("Starting text extraction for document {DocumentId}", document.Id);

                // Ajouter un délai pour s'assurer que le fichier est bien fermé avant d'essayer de l'extraire
                await Task.Delay(500);

                // Vérifier si le fichier existe
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.StoragePath);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Le fichier {filePath} n'existe pas");
                }

                _logger.LogInformation("Extracting text from file: {FilePath}", filePath);
                var text = await _textExtractor.ExtractTextAsync(document);
                _logger.LogInformation("Text extraction completed for document {DocumentId}, extracted {TextLength} characters",
                    document.Id, text?.Length ?? 0);

                // Créer un nouveau scope pour obtenir une nouvelle instance de DbContext
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Récupérer le document depuis la base de données
                    var dbDocument = await dbContext.Documents.FindAsync(document.Id);
                    if (dbDocument != null)
                    {
                        _logger.LogInformation("Updating document {DocumentId} with extracted text", document.Id);
                        dbDocument.ExtractedText = text;
                        dbDocument.ProcessingComplete = true;

                        dbContext.Update(dbDocument);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Document {DocumentId} updated successfully", document.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Document {DocumentId} not found when trying to update text", document.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document {DocumentId}", document.Id);

                // Également utiliser un nouveau scope pour mettre à jour le document avec l'erreur
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var dbDocument = await dbContext.Documents.FindAsync(document.Id);
                    if (dbDocument != null)
                    {
                        _logger.LogInformation("Updating document {DocumentId} with error status", document.Id);
                        dbDocument.ProcessingError = ex.Message;
                        dbDocument.ProcessingComplete = true;

                        dbContext.Update(dbDocument);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Document {DocumentId} error status updated", document.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Document {DocumentId} not found when trying to update error status", document.Id);
                    }
                }
            }
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            _logger.LogInformation("Deleting document {DocumentId}", documentId);

            var document = await _dbContext.Documents.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for deletion", documentId);
                return;
            }

            try
            {
                // Delete physical file
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.StoragePath);
                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Deleting physical file: {FilePath}", filePath);
                    // Tentative de suppression avec une méthode robuste
                    await DeleteFileWithRetryAsync(filePath);
                    _logger.LogInformation("Physical file deleted: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Physical file not found for deletion: {FilePath}", filePath);
                }

                // Delete from database
                _dbContext.Documents.Remove(document);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Document {DocumentId} deleted from database", documentId);
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
                    _logger.LogInformation("Attempt {RetryCount} to delete file: {FilePath}", retryCount + 1, filePath);
                    File.Delete(filePath);
                    success = true;
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to delete file after {RetryCount} attempts: {FilePath}", retryCount, filePath);
                        throw;
                    }

                    _logger.LogWarning(ex, "Retry {RetryCount}/{MaxRetries} deleting file: {FilePath}", retryCount, maxRetries, filePath);
                    await Task.Delay(100 * retryCount); // Backoff exponentiel
                }
            }
        }
    }
}