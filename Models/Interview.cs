using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPortal.Models
{
    public enum InterviewRound
    {
        [Display(Name = "Screening Round")]
        Screening,
        [Display(Name = "Technical Round")]
        Technical,
        [Display(Name = "Practical Round")]
        Practical,
        [Display(Name = "Managerial Round")]
        Managerial,
        [Display(Name = "HR Round")]
        HR,
        [Display(Name = "Final Round")]
        Final
    }

    public class Interview
    {
        public int Id { get; set; }

        [Required]
        public DateTime ScheduledTime { get; set; }

        public InterviewRound Round { get; set; }

        public string? Notes { get; set; }

        // Foreign Key to Candidate
        public int CandidateId { get; set; }
        
        [ForeignKey("CandidateId")]
        public Candidate? Candidate { get; set; }

        // Foreign Key to Interviewer
        public string InterviewerId { get; set; } = string.Empty;
        
        [ForeignKey("InterviewerId")]
        public ApplicationUser? Interviewer { get; set; }

        // One-to-One relationship with Feedback
        public Feedback? Feedback { get; set; }
    }
}
