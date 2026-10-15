namespace RecruitmentPortal.Models.DTOs
{
    public class KnowledgeBaseOptions
    {
        public const string SectionName = "KnowledgeBase";

        public string StoragePath { get; set; } = "App_Data/KnowledgeBaseFiles";
        public int MaxFileSizeMB { get; set; } = 25;
        public int ChunkSizeTokens { get; set; } = 800;
        public int ChunkOverlapTokens { get; set; } = 120;
        public int TopK { get; set; } = 5;
    }
}
