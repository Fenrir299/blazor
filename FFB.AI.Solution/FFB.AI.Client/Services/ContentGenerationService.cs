// FFB.AI.Client/Services/ContentGenerationService.cs
using FFB.AI.Shared.DTO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FFB.AI.Client.Services
{
    public class ContentGenerationService : IContentGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;

        public ContentGenerationService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("FFB.API");
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<ContentGenerationResponse> GenerateContentAsync(ContentGenerationRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("contentgeneration", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ContentGenerationResponse>(content, _options);
        }

        public async Task<GeneratedContentDTO> GetGeneratedContentByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"contentgeneration/{id}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeneratedContentDTO>(content, _options);
        }

        public async Task<IEnumerable<GeneratedContentDTO>> GetGeneratedContentsForDocumentAsync(int documentId)
        {
            var response = await _httpClient.GetAsync($"contentgeneration/document/{documentId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<GeneratedContentDTO>>(content, _options);
        }

        public async Task<GeneratedContentDTO> UpdateGeneratedContentAsync(int id, string title, string content)
        {
            var updateRequest = new { Title = title, Content = content };
            var json = JsonSerializer.Serialize(updateRequest);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"contentgeneration/{id}", stringContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeneratedContentDTO>(responseContent, _options);
        }
    }
}