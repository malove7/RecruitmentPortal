using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models
{
    public class KnowledgeBaseChunk
    {
        public int Id { get; set; }

        public int KnowledgeBaseDocumentId { get; set; }
        public KnowledgeBaseDocument? KnowledgeBaseDocument { get; set; }

        public int ChunkIndex { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int? PageNumber { get; set; }

        public int TokenCount { get; set; }

        [StringLength(150)]
        public string? VectorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
