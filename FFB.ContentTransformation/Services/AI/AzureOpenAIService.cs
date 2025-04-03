// Services/AI/AzureOpenAIService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.AI.OpenAI;
using System.Text;
using System.Collections.Generic;

namespace FFB.ContentTransformation.Services.AI
{
    /// <summary>
    /// Service for interacting with Azure OpenAI
    /// </summary>
    public class AzureOpenAIService : IAIService
    {
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;

        public AzureOpenAIService(
            IConfiguration configuration,
            ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;

            try
            {
                // Get configuration values
                var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException("AzureOpenAI:Endpoint configuration is missing");
                var key = configuration["AzureOpenAI:Key"] ?? throw new ArgumentNullException("AzureOpenAI:Key configuration is missing");
                _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? throw new ArgumentNullException("AzureOpenAI:DeploymentName configuration is missing");

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_deploymentName))
                {
                    throw new ArgumentException("Azure OpenAI configuration is missing or incomplete");
                }

                // Initialize Azure OpenAI client
                _client = new OpenAIClient(
                    new Uri(endpoint),
                    new AzureKeyCredential(key));

                _logger.LogInformation("Azure OpenAI service initialized with deployment: {DeploymentName}", _deploymentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                throw new InvalidOperationException("Failed to initialize Azure OpenAI client", ex);
            }
        }

        public async Task<string> GetCompletionAsync(string prompt, int maxTokens = 1000)
        {
            try
            {
                _logger.LogInformation("Requesting completion from Azure OpenAI with {MaxTokens} max tokens", maxTokens);

                // Create chat completion options
                var chatCompletionOptions = new ChatCompletionsOptions();

                // Ajouter les messages manuellement pour éviter les problèmes de nullabilité
                chatCompletionOptions.Messages.Add(new ChatRequestSystemMessage("You are a helpful assistant that transforms content according to instructions. You follow the user's instructions precisely."));
                chatCompletionOptions.Messages.Add(new ChatRequestUserMessage(prompt));

                // Configurer les autres options
                chatCompletionOptions.MaxTokens = maxTokens;
                chatCompletionOptions.Temperature = 0.7f;
                chatCompletionOptions.DeploymentName = _deploymentName;

                // Request the completion
                _logger.LogDebug("Sending request to {DeploymentName}", _deploymentName);
                var response = await _client.GetChatCompletionsAsync(chatCompletionOptions);
                var completion = response.Value;

                if (completion.Choices.Count > 0)
                {
                    var result = completion.Choices[0].Message.Content;
                    _logger.LogInformation("Successfully received completion from Azure OpenAI (tokens used: {TokensUsed})",
                        completion.Usage?.TotalTokens ?? 0);
                    return result;
                }
                else
                {
                    _logger.LogWarning("No completion choices returned from Azure OpenAI");
                    return "Désolé, je n'ai pas pu générer de contenu. Veuillez réessayer.";
                }
            }
            catch (RequestFailedException rfEx)
            {
                _logger.LogError(rfEx, "Azure OpenAI request failed: {ErrorCode} - {ErrorMessage}",
                    rfEx.ErrorCode, rfEx.Message);

                // More descriptive error message based on the error code
                var errorMessage = rfEx.ErrorCode switch
                {
                    "InvalidRequest" => "Requête invalide à Azure OpenAI. Veuillez vérifier vos paramètres.",
                    "RateLimitExceeded" => "Limite de requêtes Azure OpenAI dépassée. Veuillez réessayer plus tard.",
                    "QuotaExceeded" => "Quota Azure OpenAI dépassé. Veuillez contacter l'administrateur.",
                    "ContentFilter" => "Le contenu a été filtré par les règles de modération Azure OpenAI.",
                    _ => "Erreur lors de la communication avec Azure OpenAI"
                };

                throw new Exception(errorMessage, rfEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion from Azure OpenAI");
                throw new Exception("Erreur lors de la communication avec Azure OpenAI", ex);
            }
        }
    }
}