namespace FFB.AI.Shared.DTO
{
    public class GeneratedContentDTO
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string Tone { get; set; }
        public string TargetAudience { get; set; }
        public int WordCount { get; set; }
        public DateTime GenerationDate { get; set; }
        public string GeneratedBy { get; set; }
    }
}