using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeStrength
    {
        public int Id { get; set; }

        public int AIResumeScreeningId { get; set; }
        public AIResumeScreening? AIResumeScreening { get; set; }

        [Required]
        public string Strength { get; set; } = string.Empty;
    }
}
