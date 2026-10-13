namespace RecruitmentPortal.Models.DTOs
{
    public class OpenAIOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
        public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string EmbeddingsEndpoint { get; set; } = "https://api.openai.com/v1/embeddings";
        public int TimeoutSeconds { get; set; } = 90;
        public string PromptVersion { get; set; } = "1.0";

        public string EffectiveChatModel => !string.IsNullOrWhiteSpace(ChatModel) ? ChatModel : (!string.IsNullOrWhiteSpace(Model) ? Model : "gpt-4o-mini");
        public string EffectiveEmbeddingModel => !string.IsNullOrWhiteSpace(EmbeddingModel) ? EmbeddingModel : "text-embedding-3-small";
    }
}
