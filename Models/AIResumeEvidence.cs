using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeEvidence
    {
        public int Id { get; set; }

        public int AIResumeScreeningId { get; set; }
        public AIResumeScreening? AIResumeScreening { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Evidence { get; set; } = string.Empty;
    }
}
