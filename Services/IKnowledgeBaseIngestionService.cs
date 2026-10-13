using Microsoft.AspNetCore.Http;

namespace RecruitmentPortal.Services
{
    public class IngestionResult
    {
        public bool IsSuccess { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int ChunkCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IKnowledgeBaseIngestionService
    {
        Task<IngestionResult> IngestDocumentAsync(int knowledgeBaseId, IFormFile file, string? userId = null, CancellationToken cancellationToken = default);
        Task<IngestionResult> ReindexDocumentAsync(int documentId, CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default);
        Task DeleteKnowledgeBaseAsync(int knowledgeBaseId, CancellationToken cancellationToken = default);
    }
}
