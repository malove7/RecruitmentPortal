namespace RecruitmentPortal.Services
{
    public class DocumentPageText
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class ExtractedDocument
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string FullText { get; set; } = string.Empty;
        public List<DocumentPageText> Pages { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    public interface IDocumentTextExtractor
    {
        Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
        Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
        string NormalizeText(string rawText);
    }
}
