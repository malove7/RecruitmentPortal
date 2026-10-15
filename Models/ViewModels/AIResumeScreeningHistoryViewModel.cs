using Microsoft.AspNetCore.Mvc.Rendering;

namespace RecruitmentPortal.Models.ViewModels
{
    public class AIResumeScreeningHistoryViewModel
    {
        public List<AIResumeScreeningHistoryItemViewModel> Screenings { get; set; } = new();

        public int? SelectedJobPositionId { get; set; }
        public string? SearchTerm { get; set; }

        public IEnumerable<SelectListItem> JobPositions { get; set; } = new List<SelectListItem>();
    }

    public class AIResumeScreeningHistoryItemViewModel
    {
        public int Id { get; set; }
        public string ResumeFileName { get; set; } = string.Empty;
        public string ResumeFilePath { get; set; } = string.Empty;
        public int JobPositionId { get; set; }
        public string JobPositionTitle { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string ScreenedBy { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
