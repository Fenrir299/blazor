Guide de démarrage rapide - FFB Content Transformation POC
Ce guide vous aidera à mettre en place et à exécuter rapidement l'application POC de transformation de contenu pour la FFB.
Prérequis

.NET 8.0 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
Visual Studio Code avec les extensions :

C# Dev Kit (Microsoft)
Fluent UI (Microsoft)

Git (optionnel)
PostgreSQL (optionnel, car l'application utilise une base de données en mémoire pour le POC)

Installation

Clonez ou téléchargez le code source dans un dossier local.
Ouvrez le dossier avec Visual Studio Code.
Ouvrez un terminal dans Visual Studio Code (Menu Terminal > New Terminal) et exécutez les commandes suivantes :

bashCopiercd FFB.ContentTransformation
dotnet restore
Configuration
Pour le POC, l'application utilise :

Une base de données en mémoire (aucune configuration nécessaire)
Un service d'IA générative mock (aucune clé API réelle nécessaire)

Si vous souhaitez connecter l'application à Azure OpenAI pour des résultats réels, modifiez les paramètres dans appsettings.json :
jsonCopier"AzureOpenAI": {
"Endpoint": "https://votre-ressource.openai.azure.com/",
"Key": "votre-clé-api",
"DeploymentName": "votre-modèle-déployé"
}
Exécution de l'application
Dans le terminal, exécutez la commande suivante :
bashCopierdotnet watch run
Cela lancera l'application et ouvrira automatiquement votre navigateur par défaut à l'adresse https://localhost:5001.
Fonctionnalités du POC
L'application propose deux cas d'usage principaux :

1. Déclinaison de contenu
   Cette fonctionnalité permet de :

Charger des documents (PDF, DOCX, TXT)
Sélectionner un type de cible (Article Web, LinkedIn, Email)
Choisir un format (Court, Moyen, Long)
Ajouter des instructions complémentaires
Générer du contenu adapté à partir du document source
Copier ou télécharger le contenu généré

2. Recherche dans un corpus documentaire
   Cette fonctionnalité propose :

Une interface de chat pour poser des questions sur les documents
Des réponses contextuelles basées sur le contenu des documents
Une recherche sémantique avec extraction d'informations pertinentes

Remarques pour la démonstration

Pour le POC, des documents exemples sont préchargés.
Si vous rencontrez des problèmes avec le document PDF de la proposition commerciale, utilisez le document texte comme alternative.
Les contenus générés sont des exemples prédéfinis pour la démonstration.

Support et questions
Pour toute question technique concernant ce POC, veuillez contacter l'équipe de développement.
