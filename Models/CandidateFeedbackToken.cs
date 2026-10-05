using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPortal.Models
{
    public class CandidateFeedbackToken
    {
        public int Id { get; set; }

        public int CandidateEvaluationFormId { get; set; }

        [ForeignKey("CandidateEvaluationFormId")]
        public CandidateEvaluationForm? CandidateEvaluationForm { get; set; }

        [Required]
        [StringLength(100)]
        public string Token { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FeedbackType { get; set; } // "Technical", "Practical", or null

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }
    }
}
