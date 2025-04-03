// Services/DocumentProcessing/DocumentTextExtractor.cs
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using FFB.ContentTransformation.Data.Entities;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FFB.ContentTransformation.Services.DocumentProcessing
{
    /// <summary>
    /// Service for extracting text from various document formats
    /// </summary>
    public class DocumentTextExtractor
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentTextExtractor> _logger;

        public DocumentTextExtractor(
            IWebHostEnvironment environment,
            ILogger<DocumentTextExtractor> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> ExtractTextAsync(Data.Entities.Document document)
        {
            _logger.LogInformation("Document {DocumentId}: Démarrage de l'extraction de texte pour le fichier {FileName}",
                document.Id, document.FileName);

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.StoragePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Document {DocumentId}: Fichier non trouvé {FilePath}", document.Id, filePath);
                throw new FileNotFoundException($"Fichier {filePath} non trouvé");
            }

            _logger.LogInformation("Document {DocumentId}: Détection du type de fichier {FileType}",
                document.Id, document.FileType);

            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            string extractedText;

            try
            {
                extractedText = extension switch
                {
                    ".pdf" => await ExtractTextFromPdfAsync(filePath),
                    ".docx" => await ExtractTextFromDocxAsync(filePath),
                    ".txt" => await ReadTextFileWithSharingAsync(filePath),
                    // Pour les autres types de fichiers, nous utiliserons une extraction simple
                    _ => "Type de fichier non pris en charge pour l'extraction de texte"
                };

                _logger.LogInformation("Document {DocumentId}: Extraction terminée, {TextLength} caractères extraits",
                    document.Id, extractedText?.Length ?? 0);

                return extractedText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document {DocumentId}: Erreur lors de l'extraction de texte", document.Id);
                throw new Exception($"Erreur lors de l'extraction de texte: {ex.Message}", ex);
            }
        }
        private async Task<string> ReadTextFileWithSharingAsync(string filePath)
        {
            try
            {
                // Utilisation de FileShare.ReadWrite pour permettre l'accès concurrent
                using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading text file: {FilePath}", filePath);
                return $"Error extracting text: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            // Use iText7 to extract text from PDF
            var text = new StringBuilder();

            // This needs to be run synchronously due to iText7 API
            return await Task.Run(() =>
            {
                try
                {
                    // Utilisation de FileShare.ReadWrite pour permettre l'accès concurrent
                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);
                    using var pdfReader = new PdfReader(fileStream);
                    using var pdfDocument = new PdfDocument(pdfReader);

                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        var strategy = new SimpleTextExtractionStrategy();
                        var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                        text.AppendLine(pageText);
                    }

                    return text.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
                    return $"Error extracting text: {ex.Message}";
                }
            });
        }

        private async Task<string> ExtractTextFromDocxAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();
                try
                {
                    // Utilisation de FileShare.ReadWrite pour permettre l'accès concurrent
                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);

                    using var wordDocument = WordprocessingDocument.Open(fileStream, false);

                    // Extraire le texte du document principal
                    if (wordDocument.MainDocumentPart != null)
                    {
                        var docText = ExtractTextFromWordDocumentPart(wordDocument.MainDocumentPart);
                        text.Append(docText);
                    }

                    // Extraire le texte des en-têtes et pieds de page
                    if (wordDocument.MainDocumentPart?.HeaderParts != null)
                    {
                        foreach (var headerPart in wordDocument.MainDocumentPart.HeaderParts)
                        {
                            var headerText = ExtractTextFromWordDocumentPart(headerPart);
                            if (!string.IsNullOrWhiteSpace(headerText))
                            {
                                text.Append("Header: ").AppendLine(headerText);
                            }
                        }
                    }

                    if (wordDocument.MainDocumentPart?.FooterParts != null)
                    {
                        foreach (var footerPart in wordDocument.MainDocumentPart.FooterParts)
                        {
                            var footerText = ExtractTextFromWordDocumentPart(footerPart);
                            if (!string.IsNullOrWhiteSpace(footerText))
                            {
                                text.Append("Footer: ").AppendLine(footerText);
                            }
                        }
                    }

                    return text.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from DOCX: {FilePath}", filePath);
                    return $"Error extracting text: {ex.Message}";
                }
            });
        }

        private string ExtractTextFromWordDocumentPart(OpenXmlPart part)
        {
            var stringBuilder = new StringBuilder();

            if (part.RootElement != null)
            {
                // Parcourir tous les éléments de type Text et ajouter leur contenu textuel
                foreach (var text in part.RootElement.Descendants<Text>())
                {
                    stringBuilder.Append(text.Text);
                }

                // Parcourir tous les éléments de type Break et ajouter des sauts de ligne
                foreach (var br in part.RootElement.Descendants<Break>())
                {
                    stringBuilder.AppendLine();
                }

                // Parcourir tous les paragraphes pour ajouter des sauts de ligne entre eux
                foreach (var paragraph in part.RootElement.Descendants<Paragraph>())
                {
                    stringBuilder.AppendLine();
                }
            }

            return stringBuilder.ToString();
        }
    }
}