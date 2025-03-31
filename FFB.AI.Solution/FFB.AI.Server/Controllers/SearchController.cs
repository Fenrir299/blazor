// FFB.AI.Server/Controllers/SearchController.cs
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
    public class SearchController : ControllerBase
    {
        private readonly IDocumentSearchService _documentSearchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IDocumentSearchService documentSearchService,
            ILogger<SearchController> logger)
        {
            _documentSearchService = documentSearchService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<DocumentSearchResponse>> Search(DocumentSearchRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var response = await _documentSearchService.SearchAsync(request, userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche");
                return StatusCode(500, "Une erreur est survenue lors de la recherche");
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<SearchQuery>>> GetSearchHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var history = await _documentSearchService.GetUserSearchHistoryAsync(userId);
            return Ok(history);
        }
    }
}