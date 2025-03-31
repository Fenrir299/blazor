// FFB.AI.Client/Services/SearchService.cs
using FFB.AI.Shared.DTO;
using System.Net.Http.Json;
using System.Text.Json;

namespace FFB.AI.Client.Services
{
    public class SearchService : ISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;

        public SearchService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("FFB.API");
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("search", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DocumentSearchResponse>(content, _options);
        }

        public async Task<IEnumerable<SearchQueryDTO>> GetSearchHistoryAsync()
        {
            var response = await _httpClient.GetAsync("search/history");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<SearchQueryDTO>>(content, _options);
        }
    }
}