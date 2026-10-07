using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeScreening
    {
        public int Id { get; set; }

        public int JobPositionId { get; set; }
        public JobPosition? JobPosition { get; set; }

        [Required]
        [StringLength(255)]
        public string ResumeFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ResumeFilePath { get; set; } = string.Empty;

        public int OverallScore { get; set; }

        [Required]
        [StringLength(100)]
        public string Recommendation { get; set; } = string.Empty;

        [Required]
        public string RecruiterSummary { get; set; } = string.Empty;

        // Snapshots of the Job Position at the time of screening
        [StringLength(200)]
        public string JobTitleSnapshot { get; set; } = string.Empty;
        public string JobDescriptionSnapshot { get; set; } = string.Empty;
        public string JobRequirementsSnapshot { get; set; } = string.Empty;

        // Experience Analysis fields
        public string? RequiredExperience { get; set; }
        public string? CandidateExperience { get; set; }
        public string? ExperienceStatus { get; set; }
        public string? ExperienceComments { get; set; }

        [StringLength(100)]
        public string AIModel { get; set; } = string.Empty;

        [StringLength(50)]
        public string PromptVersion { get; set; } = "1.0";

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? RawJsonResponse { get; set; }

        // Navigation Collections
        public ICollection<AIResumeSkillEvaluation> Skills { get; set; } = new List<AIResumeSkillEvaluation>();
        public ICollection<AIResumeStrength> Strengths { get; set; } = new List<AIResumeStrength>();
        public ICollection<AIResumeEvidence> EvidenceExtracted { get; set; } = new List<AIResumeEvidence>();
        public ICollection<AIResumeConcern> Concerns { get; set; } = new List<AIResumeConcern>();
        public ICollection<AIResumeGap> Gaps { get; set; } = new List<AIResumeGap>();
        public ICollection<AIResumeFollowUpQuestion> FollowUpQuestions { get; set; } = new List<AIResumeFollowUpQuestion>();
    }
}
