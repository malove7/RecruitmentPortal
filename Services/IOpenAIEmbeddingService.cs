namespace RecruitmentPortal.Services
{
    public interface IOpenAIEmbeddingService
    {
        Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
        Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    }
}
