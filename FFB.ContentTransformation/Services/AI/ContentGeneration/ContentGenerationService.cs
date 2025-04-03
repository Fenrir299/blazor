// Services/AI/ContentGeneration/ContentGenerationService.cs
using System;
using System.Text;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data;
using FFB.ContentTransformation.Data.Entities;
using FFB.ContentTransformation.Models;
using FFB.ContentTransformation.Services.DocumentProcessing;
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
                // Get the document text
                var documentText = await _documentProcessingService.ExtractTextFromDocumentAsync(document);

                // Build the prompt
                var prompt = BuildPrompt(documentText, options);

                // Get completion from AI service
                var content = await _aiService.GetCompletionAsync(prompt, GetMaxTokensByFormat(options.Format));

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content for document {DocumentId}", document.Id);
                throw;
            }
        }

        private string BuildPrompt(string documentText, ContentGenerationOptions options)
        {
            var sb = new StringBuilder();

            // If text is too long, truncate it to a reasonable length
            var truncatedText = documentText.Length > 6000 ? documentText.Substring(0, 6000) + "..." : documentText;
            _logger.LogInformation("Document text (truncated): {TruncatedText}", truncatedText);

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
    }
}