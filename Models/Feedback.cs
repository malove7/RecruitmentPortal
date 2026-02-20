using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentPortal.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } // 1-5 scale

        [Required]
        public string Comments { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign Key to Interview
        public int InterviewId { get; set; }
        
        [ForeignKey("InterviewId")]
        public Interview? Interview { get; set; }
    }
}
