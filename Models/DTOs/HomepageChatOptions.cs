namespace RecruitmentPortal.Models.DTOs
{
    public class HomepageChatOptions
    {
        public const string SectionName = "HomepageChat";

        public bool Enabled { get; set; } = true;
        public int DefaultKnowledgeBaseId { get; set; } = 1;
        public int MaxMessageLength { get; set; } = 1000;
        public int MaxAudioSizeMB { get; set; } = 10;
        public bool EnableVoiceInput { get; set; } = true;
        public string FallbackMessage { get; set; } = "I could not find this in the knowledge base. Please contact the recruitment team for help.";
    }
}
