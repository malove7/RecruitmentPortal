namespace RecruitmentPortal.Services
{
    public class TranscriptionResult
    {
        public bool IsSuccess { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public interface IOpenAITranscriptionService
    {
        Task<TranscriptionResult> TranscribeAudioAsync(
            Stream audioStream,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default,
            string? prompt = null,
            string? language = null);
    }
}
