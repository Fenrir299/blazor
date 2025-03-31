// Classe DTO pour les recherches (à ajouter dans FFB.AI.Shared/DTO)
namespace FFB.AI.Shared.DTO
{
    public class SearchQueryDTO
    {
        public int Id { get; set; }
        public string Query { get; set; }
        public DateTime QueryDate { get; set; }
    }
}