namespace FFB.AI.Shared.DTO
{
    public class DocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public bool IsProcessed { get; set; }
        public List<DocumentSegmentDTO> Segments { get; set; } = new();
    }

    public class DocumentSegmentDTO
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; }
        public int PageNumber { get; set; }
        public int SegmentNumber { get; set; }
    }
}