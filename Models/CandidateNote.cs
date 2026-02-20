using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPortal.Models
{
    public class CandidateNote
    {
        public int Id { get; set; }

        [Required]
        public string NoteText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }
    }
}
