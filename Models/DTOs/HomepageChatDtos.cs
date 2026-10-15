namespace RecruitmentPortal.Models.DTOs
{
    public class HomepageChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }
    }

    public class HomepageChatMessageResponse
    {
        public bool Success { get; set; }
        public int? SessionId { get; set; }
        public int? MessageId { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<HomepageCitationDto> Citations { get; set; } = new();
        public bool IsFallback { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class HomepageCitationDto
    {
        public int ChunkId { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public int ChunkIndex { get; set; }
    }

    public class HomepageVoiceTranscribeResponse
    {
        public bool Success { get; set; }
        public string? Transcript { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class HomepageChatConfigDto
    {
        public bool Enabled { get; set; }
        public bool EnableVoiceInput { get; set; }
        public int MaxMessageLength { get; set; }
        public int MaxAudioSizeMB { get; set; }
        public string FallbackMessage { get; set; } = string.Empty;
    }
}
