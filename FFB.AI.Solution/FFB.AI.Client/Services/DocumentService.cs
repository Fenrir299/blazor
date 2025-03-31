// FFB.AI.Client/Services/DocumentService.cs
using FFB.AI.Shared.DTO;
using System.Net.Http.Json;
using System.Text.Json;

namespace FFB.AI.Client.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;

        public DocumentService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("FFB.API");
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IEnumerable<DocumentDTO>> GetAllDocumentsAsync()
        {
            var response = await _httpClient.GetAsync("documents");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<DocumentDTO>>(content, _options);
        }

        public async Task<DocumentDTO> GetDocumentByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"documents/{id}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DocumentDTO>(content, _options);
        }

        public async Task<DocumentDTO> UploadDocumentAsync(MultipartFormDataContent formData)
        {
            var response = await _httpClient.PostAsync("documents/upload", formData);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DocumentDTO>(content, _options);
        }

        public async Task<DocumentDTO> ProcessDocumentAsync(int id)
        {
            var response = await _httpClient.PostAsync($"documents/{id}/process", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DocumentDTO>(content, _options);
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"documents/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
