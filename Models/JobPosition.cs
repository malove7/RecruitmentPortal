using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class JobPosition
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string Requirements { get; set; } // Skills required

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation property for related Candidates
        public ICollection<Candidate>? Candidates { get; set; }
    }
}
