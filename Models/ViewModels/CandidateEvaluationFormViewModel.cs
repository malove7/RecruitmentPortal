using System.ComponentModel.DataAnnotations;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Models.ViewModels
{
    public class CandidateEvaluationFormViewModel
    {
        public int Id { get; set; }

        public Guid SubmissionToken { get; set; }

        // Personal Information
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email ID")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Interview Date")]
        [DataType(DataType.Date)]
        public DateTime? InterviewDate { get; set; } = DateTime.Today;

        [Display(Name = "Interview Time")]
        public string? InterviewTime { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position Applied For is required.")]
        [Display(Name = "Position Applied For")]
        public string PositionAppliedFor { get; set; } = string.Empty;

        [Display(Name = "Total Experience (Years)")]
        public string? TotalExperience { get; set; }

        [Display(Name = "Relevant Industry Experience")]
        public string? RelevantIndustryExperience { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Residence Address")]
        public string? ResidenceAddress { get; set; }

        [Display(Name = "Married")]
        public bool? Married { get; set; }

        [Display(Name = "Child")]
        public bool? Child { get; set; }

        [Display(Name = "Name of Current Organization")]
        public string? CurrentOrganization { get; set; }

        [Display(Name = "Expected CTC")]
        public string? ExpectedCTC { get; set; }

        [Display(Name = "Offered CTC")]
        public string? OfferedCTC { get; set; }

        [Display(Name = "Joining Date")]
        [DataType(DataType.Date)]
        public DateTime? JoiningDate { get; set; }

        [Display(Name = "Notice Period")]
        public string? NoticePeriod { get; set; }

        [Display(Name = "Have you ever been interviewed by Spacestem?")]
        public bool? PreviouslyInterviewed { get; set; }

        // Work Experience (3 records)
        public List<WorkExperienceEntry> WorkExperiences { get; set; } = new List<WorkExperienceEntry>
        {
            new WorkExperienceEntry { RecordNumber = 1 },
            new WorkExperienceEntry { RecordNumber = 2 },
            new WorkExperienceEntry { RecordNumber = 3 }
        };

        // Education (3 records)
        public List<EducationEntry> EducationRecords { get; set; } = new List<EducationEntry>
        {
            new EducationEntry { RecordNumber = 1 },
            new EducationEntry { RecordNumber = 2 },
            new EducationEntry { RecordNumber = 3 }
        };

        // Declaration
        [Display(Name = "I declare that the information provided is true and correct.")]
        public bool Declaration { get; set; }

        [Display(Name = "Digital Signature (Type Full Name)")]
        public string? DigitalSignature { get; set; }

        // HR Evaluation
        [Range(0, 10)]
        [Display(Name = "Communication Skills")]
        public int CommunicationSkills { get; set; } = 5;

        [Range(0, 10)]
        [Display(Name = "Confidence")]
        public int Confidence { get; set; } = 5;

        [Display(Name = "HR Comments")]
        public string? HRComments { get; set; }

        [Display(Name = "HR Signature")]
        public string? HRSignature { get; set; }

        [Display(Name = "Evaluation Status")]
        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Pending;

        // Technical Evaluation
        [Display(Name = "Technical Comments")]
        public string? TechnicalComments { get; set; }

        [Display(Name = "Technical Reviewer Signature")]
        public string? TechnicalReviewerSignature { get; set; }

        public List<TechnicalEvaluationEntry> TechnicalEvaluations { get; set; } = new List<TechnicalEvaluationEntry>();

        // Other
        [Display(Name = "Other Comments")]
        public string? OtherComments { get; set; }
    }

    public class WorkExperienceEntry
    {
        public int RecordNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? Designation { get; set; }
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        public string? DurationOfWork { get; set; }
        public string? LastCTCPerAnnum { get; set; }
    }

    public class EducationEntry
    {
        public int RecordNumber { get; set; }
        public string? DegreeCourse { get; set; }
        public string? YearOfPassing { get; set; }
        public string? DivisionPercentage { get; set; }
    }

    public class TechnicalEvaluationEntry
    {
        public int Id { get; set; }
        public int RecordNumber { get; set; }

        [Display(Name = "Technical Reviewer Signature")]
        public string? TechnicalReviewerSignature { get; set; }

        [Display(Name = "Technical Comments")]
        public string? TechnicalComments { get; set; }

        [Display(Name = "Evaluation Date")]
        public DateTime? EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
