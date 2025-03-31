// FFB.AI.Client/Services/ISearchService.cs
using FFB.AI.Shared.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FFB.AI.Client.Services
{
    public interface ISearchService
    {
        Task<DocumentSearchResponse> SearchAsync(DocumentSearchRequest request);
        Task<IEnumerable<SearchQueryDTO>> GetSearchHistoryAsync();
    }
}
