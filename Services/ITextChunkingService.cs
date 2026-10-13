namespace RecruitmentPortal.Services
{
    public class TextChunk
    {
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public int TokenCount { get; set; }
    }

    public interface ITextChunkingService
    {
        List<TextChunk> ChunkDocument(ExtractedDocument document, int chunkSizeTokens = 800, int chunkOverlapTokens = 120);
        List<TextChunk> ChunkText(string text, int? pageNumber = null, int chunkSizeTokens = 800, int chunkOverlapTokens = 120);
        int EstimateTokenCount(string text);
    }
}
