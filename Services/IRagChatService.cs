namespace RecruitmentPortal.Services
{
    public class RagCitation
    {
        public int ChunkId { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public double Score { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class RagChatResponse
    {
        public bool IsSuccess { get; set; }
        public int SessionId { get; set; }
        public int MessageId { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<RagCitation> Citations { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public interface IRagChatService
    {
        Task<RagChatResponse> AskQuestionAsync(int knowledgeBaseId, string question, int? sessionId = null, string? userId = null, CancellationToken cancellationToken = default);
        string BuildUserPrompt(string question, IEnumerable<RagCitation> citations);
    }
}
