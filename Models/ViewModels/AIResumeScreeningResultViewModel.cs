using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Models.ViewModels
{
    public class AIResumeScreeningResultViewModel
    {
        public int Id { get; set; }
        public int JobPositionId { get; set; }
        public string JobPositionTitle { get; set; } = string.Empty;
        public string JobDescriptionSnapshot { get; set; } = string.Empty;
        public string JobRequirementsSnapshot { get; set; } = string.Empty;

        public string ResumeFileName { get; set; } = string.Empty;
        public string ResumeFilePath { get; set; } = string.Empty;

        public int OverallScore { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string RecruiterSummary { get; set; } = string.Empty;

        public DateTime ScreeningDate { get; set; }
        public string ScreenedBy { get; set; } = string.Empty;
        public string AIModel { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;

        // Experience Analysis
        public AIExperienceAnalysisDto ExperienceAnalysis { get; set; } = new();

        // Skill Evaluations
        public List<AIResumeSkillEvaluationItemViewModel> Skills { get; set; } = new();

        // Strengths
        public List<string> Strengths { get; set; } = new();

        // Evidence Extracted
        public List<AIEvidenceDto> EvidenceExtracted { get; set; } = new();

        // Concerns
        public List<string> Concerns { get; set; } = new();

        // Gaps
        public List<string> Gaps { get; set; } = new();

        // Follow-Up Questions (Sorted High priority first)
        public List<AIFollowUpQuestionDto> FollowUpQuestions { get; set; } = new();
    }

    public class AIResumeSkillEvaluationItemViewModel
    {
        public string Skill { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; } = 10;
        public string Status { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Evidence { get; set; } = new();
        public List<string> Gaps { get; set; } = new();
    }
}
