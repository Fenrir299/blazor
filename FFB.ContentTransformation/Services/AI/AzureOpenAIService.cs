// Services/AI/AzureOpenAIService.cs - version simplifiée
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FFB.ContentTransformation.Services.AI
{
    /// <summary>
    /// Service for interacting with Azure OpenAI (mock implementation for POC)
    /// </summary>
    public class AzureOpenAIService : IAIService
    {
        private readonly ILogger<AzureOpenAIService> _logger;

        public AzureOpenAIService(
            IConfiguration configuration,
            ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetCompletionAsync(string prompt, int maxTokens = 1000)
        {
            // Pour le POC, nous utilisons une réponse simulée
            _logger.LogInformation("Using mock OpenAI service for POC");

            // Simuler un délai réseau
            await Task.Delay(1500);

            return GetMockResponse(prompt);
        }

        private string GetMockResponse(string prompt)
        {
            if (prompt.Contains("article web") || prompt.Contains("article"))
            {
                return "# La FFB s'engage dans la révolution de l'IA générative\n\n" +
                       "## Un partenariat prometteur avec OnePoint\n\n" +
                       "La Fédération Française du Bâtiment (FFB) franchit une étape décisive dans sa transformation numérique en s'associant avec OnePoint pour explorer les potentialités des intelligences artificielles génératives. Cette initiative s'articule autour de deux cas d'usage concrets qui promettent d'améliorer significativement l'efficacité opérationnelle des équipes.\n\n" +
                       "## Deux cas d'usage innovants\n\n" +
                       "Le premier prototype vise à transformer des contenus techniques en formats adaptés à différentes cibles. Qu'il s'agisse de vulgariser des informations complexes ou de les reformater pour différents canaux de communication (articles web, posts LinkedIn ou emails), cette solution permettra un gain de temps considérable pour les équipes de communication.\n\n" +
                       "Le second cas d'usage concerne la recherche intelligente dans un corpus documentaire. Grâce à un agent conversationnel, les collaborateurs pourront rapidement accéder aux informations pertinentes disséminées dans de nombreux documents.";
            }
            else if (prompt.Contains("LinkedIn") || prompt.Contains("post"))
            {
                return "Fière d'annoncer notre collaboration avec OnePoint pour intégrer l'IA générative dans nos processus ! 🚀\n\n" +
                       "Nous lançons deux prototypes innovants :\n" +
                       "✅ Un outil de transformation de contenus qui adapte automatiquement nos documents techniques pour différents formats et audiences\n" +
                       "✅ Un assistant de recherche intelligent pour exploiter efficacement notre base documentaire\n\n" +
                       "Cette initiative s'inscrit dans notre stratégie de transformation digitale pour gagner en efficacité opérationnelle et mieux servir nos adhérents.\n\n" +
                       "Premiers résultats attendus en mai 2025. Restez connectés !\n\n" +
                       "#Innovation #IntelligenceArtificielle #Bâtiment #Digitalisation #TransformationNumérique";
            }
            else
            {
                return "Objet : Lancement de notre projet d'IA générative avec OnePoint\n\n" +
                       "Chers collaborateurs,\n\n" +
                       "J'ai le plaisir de vous annoncer le lancement de notre projet d'expérimentation des technologies d'intelligence artificielle générative, en partenariat avec le cabinet OnePoint.\n\n" +
                       "Notre fédération s'engage ainsi dans une démarche innovante visant à simplifier notre quotidien et à gagner en efficacité opérationnelle. Deux cas d'usage ont été identifiés pour cette phase pilote :\n\n" +
                       "1. La déclinaison automatique de contenus : ce prototype nous permettra de vulgariser, résumer et adapter nos documents techniques vers différents formats (articles web, posts LinkedIn, emails) en fonction des audiences ciblées.\n\n" +
                       "2. La recherche intelligente dans notre corpus documentaire : un agent conversationnel nous aidera à localiser rapidement les informations pertinentes au sein de notre base documentaire.\n\n" +
                       "Le planning prévoit un déploiement progressif sur les mois d'avril et mai 2025, avec une première mise à disposition des outils prévue mi-mai.\n\n" +
                       "Je vous invite à partager vos questions ou suggestions concernant ce projet auprès de l'équipe digitale.\n\n" +
                       "Cordialement,\n\n" +
                       "La Direction";
            }
        }
    }
}