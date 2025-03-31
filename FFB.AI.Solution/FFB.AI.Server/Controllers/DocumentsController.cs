// FFB.AI.Server/Controllers/DocumentsController.cs
using FFB.AI.Core.Interfaces;
using FFB.AI.Core.Models;
using FFB.AI.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FFB.AI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            return Ok(document);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Document>> UploadDocument([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier n'a été fourni");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var stream = file.OpenReadStream();
            var document = await _documentService.UploadDocumentAsync(
                stream,
                file.FileName,
                file.ContentType,
                userId
            );

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }

        [HttpPost("{id}/process")]
        public async Task<ActionResult<Document>> ProcessDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            if (document.IsProcessed)
                return Ok(document);

            var processedDocument = await _documentService.ProcessDocumentAsync(id);
            return Ok(processedDocument);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            var success = await _documentService.DeleteDocumentAsync(id);

            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
