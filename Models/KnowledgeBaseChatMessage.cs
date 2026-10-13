using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class KnowledgeBaseChatMessage
    {
        public int Id { get; set; }

        public int ChatSessionId { get; set; }
        public KnowledgeBaseChatSession? ChatSession { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "user"; // "user", "assistant", "system"

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? CitationsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
