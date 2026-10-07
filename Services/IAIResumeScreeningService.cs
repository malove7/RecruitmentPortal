using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Services
{
    public class AIScreeningInput
    {
        public int JobPositionId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string JobRequirements { get; set; } = string.Empty;
        public string ResumeFileName { get; set; } = string.Empty;
        public string ResumeText { get; set; } = string.Empty;
        public string? Model { get; set; }
    }

    public class AIScreeningServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public AIScreeningResponseDto? Data { get; set; }
        public string RawJsonResponse { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
    }

    public interface IAIResumeScreeningService
    {
        Task<AIScreeningServiceResult> ScreenResumeAsync(AIScreeningInput input, CancellationToken cancellationToken = default);
    }
}
