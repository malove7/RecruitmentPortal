namespace RecruitmentPortal.Services
{
    public class PineconeVectorRecord
    {
        public string Id { get; set; } = string.Empty;
        public List<float> Values { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PineconeQueryResult
    {
        public string Id { get; set; } = string.Empty;
        public double Score { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public interface IPineconeVectorService
    {
        string BuildVectorId(int knowledgeBaseId, int documentId, int chunkIndex);
        string BuildNamespace(int knowledgeBaseId);
        Task UpsertVectorsAsync(string namespaceName, IEnumerable<PineconeVectorRecord> vectors, CancellationToken cancellationToken = default);
        Task DeleteVectorsAsync(string namespaceName, IEnumerable<string> vectorIds, CancellationToken cancellationToken = default);
        Task DeleteNamespaceAsync(string namespaceName, CancellationToken cancellationToken = default);
        Task<List<PineconeQueryResult>> QueryVectorsAsync(string namespaceName, List<float> vector, int topK = 5, CancellationToken cancellationToken = default);
    }
}
