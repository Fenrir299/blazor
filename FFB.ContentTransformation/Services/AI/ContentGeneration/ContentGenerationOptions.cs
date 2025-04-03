// Services/AI/ContentGeneration/ContentGenerationOptions.cs
using FFB.ContentTransformation.Data.Entities;
using System.Collections.Generic;

namespace FFB.ContentTransformation.Services.AI.ContentGeneration
{
    /// <summary>
    /// Options for content generation
    /// </summary>
    public class ContentGenerationOptions
    {
        /// <summary>
        /// Type of content to generate (article, post, email, etc.)
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// Format/length of the generated content
        /// </summary>
        public ContentFormat Format { get; set; }

        /// <summary>
        /// Optional custom instructions from user
        /// </summary>
        public string? CustomPrompt { get; set; }

        /// <summary>
        /// List of document IDs to process
        /// </summary>
        public List<int> DocumentIds { get; set; } = new List<int>();

        /// <summary>
        /// Maximum number of characters to include in a single prompt
        /// </summary>
        public int MaxPromptCharacters { get; set; } = 6000;

        /// <summary>
        /// Whether to use chunking for large documents
        /// </summary>
        public bool UseChunking { get; set; } = true;

        /// <summary>
        /// Strategy for handling multi-document content
        /// </summary>
        public MultiDocumentStrategy MultiDocStrategy { get; set; } = MultiDocumentStrategy.Combine;
    }

    /// <summary>
    /// Strategy for handling multiple documents
    /// </summary>
    public enum MultiDocumentStrategy
    {
        /// <summary>
        /// Combine all documents into a single prompt/request
        /// </summary>
        Combine,

        /// <summary>
        /// Process each document separately, then combine the results
        /// </summary>
        ProcessSeparately,

        /// <summary>
        /// Generate a summary for each document, then produce final content based on summaries
        /// </summary>
        SummarizeThenCombine
    }
}