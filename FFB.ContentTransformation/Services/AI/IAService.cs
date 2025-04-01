// Services/AI/IAIService.cs
using System.Threading.Tasks;

namespace FFB.ContentTransformation.Services.AI
{
    /// <summary>
    /// Interface for AI service
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Gets a completion from the AI service
        /// </summary>
        Task<string> GetCompletionAsync(string prompt, int maxTokens = 1000);
    }
}