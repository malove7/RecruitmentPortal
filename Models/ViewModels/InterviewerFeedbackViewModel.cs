using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models.ViewModels
{
    public class GenerateFeedbackLinkRequest
    {
        public int FormId { get; set; }
        public string? FeedbackType { get; set; }
    }

    public class InterviewerFeedbackViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        public int FormId { get; set; }

        // Candidate Details (Only required info to be displayed to interviewer)
        public string CandidateFullName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string PositionAppliedFor { get; set; } = string.Empty;

        // Feedback type configuration
        public string? SelectedFeedbackType { get; set; } // "Technical", "Practical", or null
        public bool IsTypePreSelected { get; set; }

        // Feedback inputs
        [Required(ErrorMessage = "Interviewer name is required.")]
        [StringLength(150, ErrorMessage = "Interviewer name cannot exceed 150 characters.")]
        [Display(Name = "Interviewer Name")]
        public string ReviewerSignature { get; set; } = string.Empty;

        [Required(ErrorMessage = "Feedback comments are required.")]
        [StringLength(1000, ErrorMessage = "Feedback comments cannot exceed 1000 characters.")]
        [Display(Name = "Feedback Comments")]
        public string Comments { get; set; } = string.Empty;

        // Error handling & validation state
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class FeedbackSuccessViewModel
    {
        public string CandidateFullName { get; set; } = string.Empty;
        public string PositionAppliedFor { get; set; } = string.Empty;
        public string FeedbackType { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
