// FFB.AI.Core/Interfaces/IDocumentService.cs
using FFB.AI.Core.Models;
using System.Threading.Tasks;

namespace FFB.AI.Core.Interfaces
{
    /// <summary>
    /// Interface pour la gestion des documents
    /// </summary>
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(Stream documentStream, string fileName, string contentType, string uploadedBy);
        Task<Document> GetDocumentByIdAsync(int id);
        Task<IEnumerable<Document>> GetAllDocumentsAsync();
        Task<bool> DeleteDocumentAsync(int id);
        Task<Document> ProcessDocumentAsync(int id);
    }
}

// FFB.AI.Core/Interfaces/IContentGenerationService.cs
using FFB.AI.Core.Models;
using FFB.AI.Shared.DTO;
using System.Threading.Tasks;

namespace FFB.AI.Core.Interfaces
{
    /// <summary>
    /// Interface pour la génération de contenu
    /// </summary>
    public interface IContentGenerationService
    {
        Task<GeneratedContent> GenerateContentAsync(ContentGenerationRequest request, string userId);
        Task<GeneratedContent> GetGeneratedContentByIdAsync(int id);
        Task<IEnumerable<GeneratedContent>> GetGeneratedContentsForDocumentAsync(int documentId);
        Task<GeneratedContent> UpdateGeneratedContentAsync(int id, string title, string content);
    }
}

// FFB.AI.Core/Interfaces/IDocumentSearchService.cs
using FFB.AI.Core.Models;
using FFB.AI.Shared.DTO;
using System.Threading.Tasks;

namespace FFB.AI.Core.Interfaces
{
    /// <summary>
    /// Interface pour la recherche dans les documents
    /// </summary>
    public interface IDocumentSearchService
    {
        Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request, string userId);
        Task<IEnumerable<SearchQuery>> GetUserSearchHistoryAsync(string userId);
    }
}

// FFB.AI.Core/Interfaces/IAzureAIService.cs
using System.Threading.Tasks;

namespace FFB.AI.Core.Interfaces
{
    /// <summary>
    /// Interface pour les services Azure AI
    /// </summary>
    public interface IAzureAIService
    {
        Task<string> ExtractTextFromDocumentAsync(Stream documentStream, string contentType);
        Task<string[]> SegmentDocumentAsync(string text, int maxSegmentLength = 1000);
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<string> GenerateContentAsync(string sourceContent, string contentType, string tone, string targetAudience, int maxWordCount, bool simplify, bool summarize);
        Task<string> GenerateAnswerFromContextAsync(string question, IEnumerable<string> contextSegments);
    }
}

// FFB.AI.Core/Interfaces/IAzureBlobStorageService.cs
using System.Threading.Tasks;

namespace FFB.AI.Core.Interfaces
{
    /// <summary>
    /// Interface pour le stockage Azure Blob
    /// </summary>
    public interface IAzureBlobStorageService
    {
        Task<string> UploadDocumentAsync(Stream documentStream, string fileName, string contentType);
        Task<Stream> DownloadDocumentAsync(string blobUrl);
        Task DeleteDocumentAsync(string blobUrl);
    }
}

// FFB.AI.Infrastructure/Services/AzureAIService.cs
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.FormRecognizer;
using FFB.AI.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFB.AI.Infrastructure.Services
{
    /// <summary>
    /// Implémentation des services Azure AI
    /// </summary>
    public class AzureAIService : IAzureAIService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly ILogger<AzureAIService> _logger;
        private readonly string _openAIDeploymentName;
        private readonly string _embeddingDeploymentName;

        public AzureAIService(IConfiguration configuration, ILogger<AzureAIService> logger)
        {
            _logger = logger;

            var openAIEndpoint = configuration["AzureOpenAI:Endpoint"];
            var openAIKey = configuration["AzureOpenAI:Key"];
            _openAIDeploymentName = configuration["AzureOpenAI:DeploymentName"];
            _embeddingDeploymentName = configuration["AzureOpenAI:EmbeddingDeploymentName"];

            var formRecognizerEndpoint = configuration["AzureFormRecognizer:Endpoint"];
            var formRecognizerKey = configuration["AzureFormRecognizer:Key"];

            _openAIClient = new OpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
            _documentAnalysisClient = new DocumentAnalysisClient(new Uri(formRecognizerEndpoint), new AzureKeyCredential(formRecognizerKey));
        }

        /// <summary>
        /// Extrait le texte d'un document à l'aide d'Azure Document Intelligence
        /// </summary>
        public async Task<string> ExtractTextFromDocumentAsync(Stream documentStream, string contentType)
        {
            try
            {
                documentStream.Position = 0;
                var response = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", documentStream);
                var result = response.Value;

                var textBuilder = new StringBuilder();
                foreach (var page in result.Pages)
                {
                    foreach (var paragraph in page.Paragraphs)
                    {
                        textBuilder.AppendLine(paragraph.Content);
                    }
                    textBuilder.AppendLine(); // Add space between pages
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction du texte du document");
                throw;
            }
        }

        /// <summary>
        /// Segmente un document en portions de texte plus petites
        /// </summary>
        public Task<string[]> SegmentDocumentAsync(string text, int maxSegmentLength = 1000)
        {
            var segments = new List<string>();
            var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentSegment = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                if (currentSegment.Length + paragraph.Length > maxSegmentLength)
                {
                    if (currentSegment.Length > 0)
                    {
                        segments.Add(currentSegment.ToString().Trim());
                        currentSegment.Clear();
                    }

                    // Si le paragraphe est plus long que maxSegmentLength, le diviser
                    if (paragraph.Length > maxSegmentLength)
                    {
                        var words = paragraph.Split(' ');
                        var tempSegment = new StringBuilder();

                        foreach (var word in words)
                        {
                            if (tempSegment.Length + word.Length + 1 > maxSegmentLength)
                            {
                                segments.Add(tempSegment.ToString().Trim());
                                tempSegment.Clear();
                            }

                            tempSegment.Append(word + " ");
                        }

                        if (tempSegment.Length > 0)
                        {
                            currentSegment.Append(tempSegment);
                        }
                    }
                    else
                    {
                        currentSegment.Append(paragraph);
                    }
                }
                else
                {
                    currentSegment.AppendLine(paragraph);
                }
            }

            if (currentSegment.Length > 0)
            {
                segments.Add(currentSegment.ToString().Trim());
            }

            return Task.FromResult(segments.ToArray());
        }

        /// <summary>
        /// Génère un embedding pour un texte donné
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var embeddingOptions = new EmbeddingsOptions(_embeddingDeploymentName, new List<string> { text });
                var response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);

                return response.Value.Data[0].Embedding.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de l'embedding");
                throw;
            }
        }

        /// <summary>
        /// Génère du contenu à partir d'un texte source
        /// </summary>
        public async Task<string> GenerateContentAsync(string sourceContent, string contentType, string tone, string targetAudience, int maxWordCount, bool simplify, bool summarize)
        {
            try
            {
                var systemPrompt = $@"Vous êtes un assistant spécialisé dans la rédaction et l'adaptation de contenu. 
Votre tâche est de {(summarize ? "résumer" : "adapter")} le contenu fourni en un {contentType} 
avec un ton {tone}, destiné à un public {targetAudience}.
{(simplify ? "Simplifiez et vulgarisez le contenu pour le rendre accessible." : "Conservez le niveau technique approprié.")}
Le contenu final ne doit pas dépasser {maxWordCount} mots.
Produisez un contenu structuré avec un titre pertinent.";

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _openAIDeploymentName,
                    Temperature = 0.7f,
                    MaxTokens = maxWordCount * 2, // Approximation de tokens par rapport aux mots
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestUserMessage(sourceContent)
                    }
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de contenu");
                throw;
            }
        }

        /// <summary>
        /// Génère une réponse à une question en utilisant les segments de contexte
        /// </summary>
        public async Task<string> GenerateAnswerFromContextAsync(string question, IEnumerable<string> contextSegments)
        {
            try
            {
                var contextText = string.Join("\n\n", contextSegments);

                var systemPrompt = @"Vous êtes un assistant intelligent de la FFB (Fédération Française du Bâtiment). 
Votre rôle est de répondre aux questions en vous basant UNIQUEMENT sur les informations fournies dans le contexte.
Si la réponse n'est pas présente dans le contexte, indiquez-le clairement et suggérez d'autres sources d'information.
Répondez de façon claire, précise et professionnelle.";

                var userPrompt = $"Contexte:\n\n{contextText}\n\nQuestion: {question}\n\nRéponse:";

                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _openAIDeploymentName,
                    Temperature = 0.0f, // Réponses plus déterministes
                    MaxTokens = 1000,
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestUserMessage(userPrompt)
                    }
                };

                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de réponse");
                throw;
            }
        }
    }
}

// FFB.AI.Infrastructure/Services/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FFB.AI.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFB.AI.Infrastructure.Services
{
    /// <summary>
    /// Implémentation des services Azure Blob Storage
    /// </summary>
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            _logger = logger;
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"];

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        /// <summary>
        /// Upload un document dans Azure Blob Storage
        /// </summary>
        public async Task<string> UploadDocumentAsync(Stream documentStream, string fileName, string contentType)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Générer un nom unique pour éviter les collisions
                var uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                documentStream.Position = 0;

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                };

                await blobClient.UploadAsync(documentStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du document");
                throw;
            }
        }

        /// <summary>
        /// Télécharge un document depuis Azure Blob Storage
        /// </summary>
        public async Task<Stream> DownloadDocumentAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var downloadStream = new MemoryStream();
                await blobClient.DownloadToAsync(downloadStream);

                downloadStream.Position = 0;
                return downloadStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du document");
                throw;
            }
        }

        /// <summary>
        /// Supprime un document d'Azure Blob Storage
        /// </summary>
        public async Task DeleteDocumentAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du document");
                throw;
            }
        }
    }
}

// FFB.AI.Infrastructure/Services/DocumentService.cs
using FFB.AI.Core.Interfaces;
using FFB.AI.Core.Models;
using FFB.AI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFB.AI.Infrastructure.Services
{
    /// <summary>
    /// Implémentation du service de gestion des documents
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly FFBDbContext _dbContext;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly IAzureAIService _azureAIService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            FFBDbContext dbContext,
            IAzureBlobStorageService blobStorageService,
            IAzureAIService azureAIService,
            ILogger<DocumentService> logger)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _azureAIService = azureAIService;
            _logger = logger;
        }

        /// <summary>
        /// Upload un document et l'enregistre en base de données
        /// </summary>
        public async Task<Document> UploadDocumentAsync(Stream documentStream, string fileName, string contentType, string uploadedBy)
        {
            try
            {
                // Upload dans Azure Blob Storage
                var blobUrl = await _blobStorageService.UploadDocumentAsync(documentStream, fileName, contentType);

                // Créer l'entrée dans la base de données
                var document = new Document
                {
                    Title = Path.GetFileNameWithoutExtension(fileName),
                    FileName = fileName,
                    ContentType = contentType,
                    BlobStorageUrl = blobUrl,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = uploadedBy,
                    IsProcessed = false
                };

                _dbContext.Documents.Add(document);
                await _dbContext.SaveChangesAsync();

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l