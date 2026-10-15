namespace RecruitmentPortal.Models.DTOs
{
    public class PineconeOptions
    {
        public const string SectionName = "Pinecone";

        public string ApiKey { get; set; } = string.Empty;
        public string IndexName { get; set; } = "recruitment-knowledgebase";
        public string Host { get; set; } = "https://recruitment-knowledgebase-6540nuo.svc.aped-4627-b74a.pinecone.io";
        public string NamespacePrefix { get; set; } = "kb";
    }
}
