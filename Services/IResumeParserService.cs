using Microsoft.AspNetCore.Http;

namespace RecruitmentPortal.Services
{
    public class ResumeParseResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string ExtractedText { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    public interface IResumeParserService
    {
        Task<ResumeParseResult> ParseResumeAsync(IFormFile? file, CancellationToken cancellationToken = default);
        Task<ResumeParseResult> ParseResumeStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    }
}
