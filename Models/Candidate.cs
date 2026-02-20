using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPortal.Models
{
    public enum CandidateStatus
    {
        Applied,
        InReview,
        InterviewScheduled,
        Offered,
        Rejected,
        Hired
    }

    public class Candidate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string Phone { get; set; }

        public string? ResumePath { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        public CandidateStatus Status { get; set; } = CandidateStatus.Applied;

        // Foreign Key to JobPosition
        public int JobPositionId { get; set; }
        
        [ForeignKey("JobPositionId")]
        public JobPosition? JobPosition { get; set; }

        [StringLength(100)]
        public string? CurrentLocation { get; set; }

        [StringLength(50)]
        public string? NoticePeriod { get; set; }

        [StringLength(100)]
        public string? Experience { get; set; }

        [StringLength(500)]
        public string? ReasonForChange { get; set; }

        [StringLength(50)]
        public string? CurrentCTC { get; set; }

        [StringLength(50)]
        public string? ExpectedCTC { get; set; }

        [StringLength(100)]
        public string? HighestEducation { get; set; }

        public DateTime? DOB { get; set; }

        public ICollection<Interview>? Interviews { get; set; }

        public ICollection<CandidateNote>? Notes { get; set; }

        [NotMapped]
        public string? NewNote { get; set; }
    }
}
