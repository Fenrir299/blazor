// Services/DocumentProcessing/DocumentTextExtractor.cs
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FFB.ContentTransformation.Data.Entities;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

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

        public async Task<string> ExtractTextAsync(Document document)
        {
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", document.StoragePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File {filePath} not found");
            }

            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => await ExtractTextFromPdfAsync(filePath),
                ".txt" => await File.ReadAllTextAsync(filePath),
                // For demo purposes, we'll use simple text extraction for other file types
                // In a production environment, we would use Azure Document Intelligence for more accuracy
                _ => "Unsupported file type for text extraction"
            };
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
                    using var pdfReader = new PdfReader(filePath);
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
    }
}