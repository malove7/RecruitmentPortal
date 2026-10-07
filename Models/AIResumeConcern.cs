using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class AIResumeConcern
    {
        public int Id { get; set; }

        public int AIResumeScreeningId { get; set; }
        public AIResumeScreening? AIResumeScreening { get; set; }

        [Required]
        public string Concern { get; set; } = string.Empty;
    }
}
