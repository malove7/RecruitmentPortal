using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class KnowledgeBaseChatSession
    {
        public int Id { get; set; }

        public int KnowledgeBaseId { get; set; }
        public KnowledgeBase? KnowledgeBase { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "New Chat";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<KnowledgeBaseChatMessage> Messages { get; set; } = new List<KnowledgeBaseChatMessage>();
    }
}
