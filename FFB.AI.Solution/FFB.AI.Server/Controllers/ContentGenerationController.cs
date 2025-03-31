// FFB.AI.Server/Controllers/ContentGenerationController.cs
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
    public class ContentGenerationController : ControllerBase
    {
        private readonly IContentGenerationService _contentGenerationService;
        private readonly ILogger<ContentGenerationController> _logger;

        public ContentGenerationController(
            IContentGenerationService contentGenerationService,
            ILogger<ContentGenerationController> logger)
        {
            _contentGenerationService = contentGenerationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ContentGenerationResponse>> GenerateContent(ContentGenerationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var generatedContent = await _contentGenerationService.GenerateContentAsync(request, userId);

                var response = new ContentGenerationResponse
                {
                    Id = generatedContent.Id,
                    Title = generatedContent.Title,
                    Content = generatedContent.Content,
                    WordCount = generatedContent.WordCount,
                    GenerationDate = generatedContent.GenerationDate
                };

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GeneratedContent>> GetGeneratedContent(int id)
        {
            var generatedContent = await _contentGenerationService.GetGeneratedContentByIdAsync(id);

            if (generatedContent == null)
                return NotFound();

            return Ok(generatedContent);
        }

        [HttpGet("document/{documentId}")]
        public async Task<ActionResult<IEnumerable<GeneratedContent>>> GetGeneratedContentsForDocument(int documentId)
        {
            var generatedContents = await _contentGenerationService.GetGeneratedContentsForDocumentAsync(documentId);
            return Ok(generatedContents);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GeneratedContent>> UpdateGeneratedContent(int id, [FromBody] ContentUpdateRequest request)
        {
            try
            {
                var generatedContent = await _contentGenerationService.UpdateGeneratedContentAsync(
                    id,
                    request.Title,
                    request.Content
                );

                return Ok(generatedContent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
    public class ContentUpdateRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}