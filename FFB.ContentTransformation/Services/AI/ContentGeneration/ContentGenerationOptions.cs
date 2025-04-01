// Services/AI/ContentGeneration/ContentGenerationOptions.cs
using FFB.ContentTransformation.Data.Entities;

namespace FFB.ContentTransformation.Services.AI.ContentGeneration
{
    /// <summary>
    /// Options for content generation
    /// </summary>
    public class ContentGenerationOptions
    {
        public ContentType ContentType { get; set; }
        public ContentFormat Format { get; set; }
        public string? CustomPrompt { get; set; }
    }
}
