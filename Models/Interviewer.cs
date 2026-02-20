using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class Interviewer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Department { get; set; }

        public ICollection<Interview>? Interviews { get; set; }
    }
}
