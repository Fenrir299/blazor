// Services/AI/ContentGeneration/ContentGenerationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data;
using FFB.ContentTransformation.Data.Entities;
using FFB.ContentTransformation.Models;
using FFB.ContentTransformation.Services.DocumentProcessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FFB.ContentTransformation.Services.AI.ContentGeneration
{
    /// <summary>
    /// Service for generating content using AI
    /// </summary>
    public class ContentGenerationService : IContentGenerationService
    {
        private readonly IAIService _aiService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ContentGenerationService> _logger;

        public ContentGenerationService(
            IAIService aiService,
            IDocumentProcessingService documentProcessingService,
            AppDbContext dbContext,
            ILogger<ContentGenerationService> logger)
        {
            _aiService = aiService;
            _documentProcessingService = documentProcessingService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> GenerateContentAsync(Document document, ContentGenerationOptions options)
        {
            try
            {
                _logger.LogInformation("Generating content for document {DocumentId} of type {ContentType}",
                    document.Id, options.ContentType);

                // Get the document text
                var documentText = await _documentProcessingService.ExtractTextFromDocumentAsync(document);

                if (string.IsNullOrEmpty(documentText))
                {
                    _logger.LogWarning("Document {DocumentId} has no extractable text", document.Id);
                    return "Le document ne contient aucun texte extractible.";
                }

                // Check if document is too long and needs chunking
                if (options.UseChunking && documentText.Length > options.MaxPromptCharacters)
                {
                    _logger.LogInformation("Document {DocumentId} is long ({Length} chars), using chunked processing",
                        document.Id, documentText.Length);
                    return await ProcessLargeDocumentAsync(documentText, options);
                }

                // For normal sized documents, process as usual
                var prompt = BuildPrompt(documentText, options);
                var content = await _aiService.GetCompletionAsync(prompt, GetMaxTokensByFormat(options.Format));

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content for document {DocumentId}", document.Id);
                throw;
            }
        }

        public async Task<string> GenerateContentAsync(IEnumerable<Document> documents, ContentGenerationOptions options)
        {
            var documentsList = documents.ToList();
            _logger.LogInformation("Generating content for {Count} documents of type {ContentType}",
                documentsList.Count, options.ContentType);

            if (!documentsList.Any())
            {
                _logger.LogWarning("No documents provided for content generation");
                return "Aucun document fourni pour la génération de contenu.";
            }

            // If only one document, use the single document method
            if (documentsList.Count == 1)
            {
                return await GenerateContentAsync(documentsList[0], options);
            }

            // Handle multiple documents based on strategy
            return options.MultiDocStrategy switch
            {
                MultiDocumentStrategy.Combine => await CombineDocumentsAndGenerateAsync(documentsList, options),
                MultiDocumentStrategy.ProcessSeparately => await ProcessDocumentsSeparatelyAsync(documentsList, options),
                MultiDocumentStrategy.SummarizeThenCombine => await SummarizeAndCombineDocumentsAsync(documentsList, options),
                _ => await CombineDocumentsAndGenerateAsync(documentsList, options)
            };
        }

        public async Task<string> GenerateContentFromDocumentIdsAsync(ContentGenerationOptions options)
        {
            // Load documents from database
            var documents = await _dbContext.Documents
                .Where(d => options.DocumentIds.Contains(d.Id))
                .ToListAsync();

            if (!documents.Any())
            {
                _logger.LogWarning("No documents found with IDs: {DocumentIds}",
                    string.Join(", ", options.DocumentIds));
                return "Aucun document trouvé avec les identifiants spécifiés.";
            }

            return await GenerateContentAsync(documents, options);
        }

        private async Task<string> CombineDocumentsAndGenerateAsync(List<Document> documents, ContentGenerationOptions options)
        {
            var combinedText = new StringBuilder();

            // Extract text from all documents and combine them
            foreach (var document in documents)
            {
                var documentText = await _documentProcessingService.ExtractTextFromDocumentAsync(document);
                if (!string.IsNullOrEmpty(documentText))
                {
                    combinedText.AppendLine($"--- DOCUMENT: {document.FileName} ---");
                    combinedText.AppendLine(documentText);
                    combinedText.AppendLine();
                }
            }

            var combinedContent = combinedText.ToString();

            // Check if combined content is too long and needs chunking
            if (options.UseChunking && combinedContent.Length > options.MaxPromptCharacters)
            {
                _logger.LogInformation("Combined documents are too long ({Length} chars), using chunked processing",
                    combinedContent.Length);
                return await ProcessLargeDocumentAsync(combinedContent, options);
            }

            // For normal sized combined content, process as usual
            var prompt = BuildPrompt(combinedContent, options);
            var generatedContent = await _aiService.GetCompletionAsync(prompt, GetMaxTokensByFormat(options.Format));

            return generatedContent;
        }

        private async Task<string> ProcessDocumentsSeparatelyAsync(List<Document> documents, ContentGenerationOptions options)
        {
            var results = new List<string>();

            // Process each document separately
            foreach (var document in documents)
            {
                var documentContent = await GenerateContentAsync(document, options);
                results.Add(documentContent);
            }

            // Combine the individual results
            var combinedOptions = new ContentGenerationOptions
            {
                ContentType = options.ContentType,
                Format = options.Format,
                CustomPrompt = options.CustomPrompt,
                MaxPromptCharacters = options.MaxPromptCharacters
            };

            var combinationPrompt = $"Combine les textes suivants en un seul {GetContentTypeDescription(options.ContentType)} cohérent:\n\n" +
                                   string.Join("\n\n--- CONTENU SUIVANT ---\n\n", results);

            var finalContent = await _aiService.GetCompletionAsync(combinationPrompt, GetMaxTokensByFormat(options.Format));

            return finalContent;
        }

        private async Task<string> SummarizeAndCombineDocumentsAsync(List<Document> documents, ContentGenerationOptions options)
        {
            var summaries = new List<string>();

            // Create a summary of each document
            foreach (var document in documents)
            {
                var documentText = await _documentProcessingService.ExtractTextFromDocumentAsync(document);
                if (!string.IsNullOrEmpty(documentText))
                {
                    var summaryPrompt = $"Résume ce texte en 1-3 paragraphes tout en conservant ses points clés:\n\n{documentText}";
                    var summary = await _aiService.GetCompletionAsync(summaryPrompt, 500);
                    summaries.Add($"Document '{document.FileName}':\n{summary}");
                }
            }

            // Combine the summaries to create the final content
            var combinedSummaries = string.Join("\n\n", summaries);
            var finalPrompt = BuildPrompt(combinedSummaries, options);
            var finalContent = await _aiService.GetCompletionAsync(finalPrompt, GetMaxTokensByFormat(options.Format));

            return finalContent;
        }

        private async Task<string> ProcessLargeDocumentAsync(string documentText, ContentGenerationOptions options)
        {
            // Split the document into logical chunks
            var chunks = SplitIntoChunks(documentText, options.MaxPromptCharacters);

            if (chunks.Count == 1)
            {
                // If only one chunk after splitting, process normally
                var prompt = BuildPrompt(chunks[0], options);
                return await _aiService.GetCompletionAsync(prompt, GetMaxTokensByFormat(options.Format));
            }

            _logger.LogInformation("Processing document in {ChunkCount} chunks", chunks.Count);

            // Step 1: Process each chunk to get a summary
            var chunkSummaries = new List<string>();
            foreach (var chunk in chunks)
            {
                var summaryPrompt = $"Résume ce texte en 1-2 paragraphes tout en conservant ses points clés:\n\n{chunk}";
                var summary = await _aiService.GetCompletionAsync(summaryPrompt, 300);
                chunkSummaries.Add(summary);

                // Add a small delay to prevent rate limiting
                await Task.Delay(100);
            }

            // Step 2: Combine all summaries
            var combinedSummaries = string.Join("\n\n", chunkSummaries);

            // Step 3: Generate final content from combined summaries
            var finalPrompt = BuildPrompt(combinedSummaries, options);
            var finalContent = await _aiService.GetCompletionAsync(finalPrompt, GetMaxTokensByFormat(options.Format));

            return finalContent;
        }

        private List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();

            // Try to split by paragraphs first (preserve paragraph structure)
            var paragraphs = Regex.Split(text, @"(\r\n|\r|\n){2,}")
                                 .Where(p => !string.IsNullOrWhiteSpace(p))
                                 .ToList();

            var currentChunk = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                // If adding this paragraph would exceed max size, add current chunk to list and start new chunk
                if (currentChunk.Length + paragraph.Length > maxChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }

                // If a single paragraph is larger than max chunk size, we need to split it
                if (paragraph.Length > maxChunkSize)
                {
                    // If there's content in current chunk, add it to chunks first
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString());
                        currentChunk.Clear();
                    }

                    // Split the large paragraph into smaller pieces
                    for (int i = 0; i < paragraph.Length; i += maxChunkSize)
                    {
                        var length = Math.Min(maxChunkSize, paragraph.Length - i);
                        chunks.Add(paragraph.Substring(i, length));
                    }
                }
                else
                {
                    // Add paragraph to current chunk
                    currentChunk.AppendLine(paragraph);
                    currentChunk.AppendLine();
                }
            }

            // Add the final chunk if not empty
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
            }

            return chunks;
        }

        private string BuildPrompt(string documentText, ContentGenerationOptions options)
        {
            var sb = new StringBuilder();

            // If text is still too long even after chunking, truncate it
            var truncatedText = documentText;
            if (documentText.Length > options.MaxPromptCharacters)
            {
                truncatedText = documentText.Substring(0, options.MaxPromptCharacters) + "...";
                _logger.LogWarning("Document text truncated from {OriginalLength} to {MaxLength} characters",
                    documentText.Length, options.MaxPromptCharacters);
            }

            // Add custom instructions based on content type
            sb.AppendLine($"Transforme le texte suivant en {GetContentTypeDescription(options.ContentType)}.");
            sb.AppendLine($"Le format doit être {GetFormatDescription(options.Format)}.");

            // Add word count guidance
            var targetWordCount = ContentFormatOptions.ApproximateWordCounts[options.Format];
            sb.AppendLine($"Vise environ {targetWordCount} mots.");

            // Add specific style instructions based on content type
            switch (options.ContentType)
            {
                case ContentType.WebArticle:
                    sb.AppendLine("Format article web avec titre accrocheur, sous-titres et paragraphes courts.");
                    break;
                case ContentType.LinkedInPost:
                    sb.AppendLine("Format post LinkedIn: professionnel, concis, avec des points clés et éventuellement des hashtags pertinents.");
                    break;
                case ContentType.Email:
                    sb.AppendLine("Format email professionnel avec objet, introduction courtoise, contenu structuré et conclusion.");
                    break;
            }

            // Add custom prompt if provided
            if (!string.IsNullOrEmpty(options.CustomPrompt))
            {
                sb.AppendLine($"Instructions supplémentaires: {options.CustomPrompt}");
            }

            // Add the source text
            sb.AppendLine("\nTEXTE SOURCE:");
            sb.AppendLine(truncatedText);

            return sb.ToString();
        }

        private string GetContentTypeDescription(ContentType contentType)
        {
            return contentType switch
            {
                ContentType.WebArticle => "un article web",
                ContentType.LinkedInPost => "un post LinkedIn",
                ContentType.Email => "un email professionnel",
                _ => "un contenu"
            };
        }

        private string GetFormatDescription(ContentFormat format)
        {
            return format switch
            {
                ContentFormat.Short => "court",
                ContentFormat.Medium => "moyen",
                ContentFormat.Long => "long",
                _ => "standard"
            };
        }

        private int GetMaxTokensByFormat(ContentFormat format)
        {
            return format switch
            {
                ContentFormat.Short => 500,
                ContentFormat.Medium => 1000,
                ContentFormat.Long => 2000,
                _ => 1000
            };
        }

        public async Task<GeneratedContent> SaveGeneratedContentAsync(Document document, string content, ContentGenerationOptions options)
        {
            var generatedContent = new GeneratedContent
            {
                DocumentId = document.Id,
                ContentType = options.ContentType,
                Format = options.Format,
                Content = content,
                CustomPrompt = options.CustomPrompt,
                GeneratedAt = DateTime.UtcNow
            };

            _dbContext.GeneratedContents.Add(generatedContent);
            await _dbContext.SaveChangesAsync();

            return generatedContent;
        }

        public async Task<GeneratedContent> SaveGeneratedContentAsync(IEnumerable<Document> documents, string content, ContentGenerationOptions options)
        {
            // Save to primary document (first in list)
            var primaryDocument = documents.FirstOrDefault();
            if (primaryDocument == null)
            {
                throw new ArgumentException("No documents provided");
            }

            var generatedContent = new GeneratedContent
            {
                DocumentId = primaryDocument.Id,
                ContentType = options.ContentType,
                Format = options.Format,
                Content = content,
                CustomPrompt = options.CustomPrompt,
                GeneratedAt = DateTime.UtcNow
            };

            _dbContext.GeneratedContents.Add(generatedContent);
            await _dbContext.SaveChangesAsync();

            return generatedContent;
        }
    }
}