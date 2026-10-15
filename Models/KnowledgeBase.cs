using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class KnowledgeBase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public ICollection<KnowledgeBaseDocument> Documents { get; set; } = new List<KnowledgeBaseDocument>();
        public ICollection<KnowledgeBaseChatSession> ChatSessions { get; set; } = new List<KnowledgeBaseChatSession>();
    }
}
