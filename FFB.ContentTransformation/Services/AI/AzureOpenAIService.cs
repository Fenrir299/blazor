// Services/AI/AzureOpenAIService.cs
using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var key = configuration["AzureOpenAI:Key"];
            _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-35-turbo";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                // For demo purposes, we'll use a mock service
                _client = null;
                return;
            }

            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        public async Task<string> GetCompletionAsync(string prompt, int maxTokens = 1000)
        {
            try
            {
                // If we don't have a client, return a mock response for the POC
                if (_client == null)
                {
                    _logger.LogWarning("Using mock OpenAI service as no credentials were provided");
                    return GetMockResponse(prompt);
                }

                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("Tu es un expert en rédaction et en vulgarisation de contenus techniques. Tu dois transformer les textes pour les adapter au format demandé tout en préservant les informations clés."),
                        new ChatRequestUserMessage(prompt)
                    },
                    MaxTokens = maxTokens,
                    Temperature = 0.7f,
                    NucleusSamplingFactor = 0.95f
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                var completion = response.Value.Choices[0].Message.Content;

                return completion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion from Azure OpenAI");
                return $"Erreur lors de la génération de contenu: {ex.Message}";
            }
        }

        private string GetMockResponse(string prompt)
        {
            // For demo purposes, we'll return a simple message
            return "Ceci est une réponse de démonstration pour le POC. Dans un environnement de production, ce texte serait généré par Azure OpenAI.\n\n" +
                   "Le contenu serait adapté en fonction du format demandé (article web, post LinkedIn ou email) et " +
                   "de la longueur souhaitée (court, moyen ou long).\n\n" +
                   "Pour activer l'intégration complète avec Azure OpenAI, veuillez configurer les clés API dans les paramètres de l'application.";
        }
    }
}