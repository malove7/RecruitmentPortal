using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeFollowUpQuestion
    {
        public int Id { get; set; }

        public int AIResumeScreeningId { get; set; }
        public AIResumeScreening? AIResumeScreening { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(100)]
        public string Skill { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"

        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Question { get; set; } = string.Empty;
    }
}
