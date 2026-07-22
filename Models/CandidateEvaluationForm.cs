using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public enum EvaluationStatus
    {
        Pending,
        Shortlisted,
        OnHold,
        Rejected
    }

    public class CandidateEvaluationForm
    {
        public int Id { get; set; }

        public Guid? SubmissionToken { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Personal Information
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        public DateTime? InterviewDate { get; set; }

        [StringLength(10)]
        public string? InterviewTime { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string PositionAppliedFor { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TotalExperience { get; set; }

        [StringLength(100)]
        public string? RelevantIndustryExperience { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Phone]
        [StringLength(20)]
        public string? ContactNumber { get; set; }

        [StringLength(500)]
        public string? ResidenceAddress { get; set; }

        public bool? Married { get; set; }

        public bool? Child { get; set; }

        [StringLength(150)]
        public string? CurrentOrganization { get; set; }

        [StringLength(50)]
        public string? ExpectedCTC { get; set; }

        [StringLength(50)]
        public string? OfferedCTC { get; set; }

        public DateTime? JoiningDate { get; set; }

        [StringLength(50)]
        public string? NoticePeriod { get; set; }

        public bool? PreviouslyInterviewed { get; set; }

        // Declaration
        public bool Declaration { get; set; }

        [StringLength(150)]
        public string? DigitalSignature { get; set; }

        // HR Evaluation
        public int? CommunicationSkills { get; set; }

        public int? Confidence { get; set; }

        [StringLength(1000)]
        public string? HRComments { get; set; }

        [StringLength(150)]
        public string? HRSignature { get; set; }

        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Pending;

        // Technical Evaluation
        [StringLength(1000)]
        public string? TechnicalComments { get; set; }

        [StringLength(150)]
        public string? TechnicalReviewerSignature { get; set; }

        // Other
        [StringLength(1000)]
        public string? OtherComments { get; set; }

        public ICollection<WorkExperienceRecord> WorkExperiences { get; set; } = new List<WorkExperienceRecord>();
        public ICollection<EducationRecord> EducationRecords { get; set; } = new List<EducationRecord>();
    }

    public class WorkExperienceRecord
    {
        public int Id { get; set; }
        public int CandidateEvaluationFormId { get; set; }
        public CandidateEvaluationForm? CandidateEvaluationForm { get; set; }

        public int RecordNumber { get; set; }

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        public string? Designation { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        public string? DurationOfWork { get; set; }

        [StringLength(50)]
        public string? LastCTCPerAnnum { get; set; }
    }

    public class EducationRecord
    {
        public int Id { get; set; }
        public int CandidateEvaluationFormId { get; set; }
        public CandidateEvaluationForm? CandidateEvaluationForm { get; set; }

        public int RecordNumber { get; set; }

        [StringLength(150)]
        public string? DegreeCourse { get; set; }

        [StringLength(10)]
        public string? YearOfPassing { get; set; }

        [StringLength(50)]
        public string? DivisionPercentage { get; set; }
    }
}
