using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RecruitmentPortal.Models.ViewModels
{
    public class AIResumeScreeningCreateViewModel
    {
        [Required(ErrorMessage = "Please select a job position.")]
        [Display(Name = "Job Position")]
        public int JobPositionId { get; set; }

        [Required(ErrorMessage = "Please upload a resume file (PDF or DOCX).")]
        [Display(Name = "Resume File")]
        public IFormFile? ResumeFile { get; set; }

        public IEnumerable<SelectListItem> JobPositions { get; set; } = new List<SelectListItem>();

        public string SelectedModel { get; set; } = "gpt-4o-mini";

        public string? ErrorMessage { get; set; }
    }
}
