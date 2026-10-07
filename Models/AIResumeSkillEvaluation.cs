using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeSkillEvaluation
    {
        public int Id { get; set; }

        public int AIResumeScreeningId { get; set; }
        public AIResumeScreening? AIResumeScreening { get; set; }

        [Required]
        [StringLength(150)]
        public string Skill { get; set; } = string.Empty;

        public int Score { get; set; }

        public int MaxScore { get; set; } = 10;

        [Required]
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;

        public double Confidence { get; set; }

        // JSON serialized list of string evidence quotes
        public string? EvidenceJson { get; set; }

        // JSON serialized list of string gaps
        public string? GapsJson { get; set; }
    }
}
