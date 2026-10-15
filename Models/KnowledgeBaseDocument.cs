using System.ComponentModel.DataAnnotations;
using RecruitmentPortal.Models.Enums;

namespace RecruitmentPortal.Models
{
    public class KnowledgeBaseDocument
    {
        public int Id { get; set; }

        public int KnowledgeBaseId { get; set; }
        public KnowledgeBase? KnowledgeBase { get; set; }

        [Required]
        [StringLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string StorageFilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FileExtension { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public KnowledgeBaseDocumentStatus Status { get; set; } = KnowledgeBaseDocumentStatus.Uploaded;

        public string? ErrorMessage { get; set; }

        public int? ExtractedCharCount { get; set; }

        public int ChunkCount { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? IndexedAt { get; set; }

        public string? UploadedByUserId { get; set; }
        public ApplicationUser? UploadedByUser { get; set; }

        public ICollection<KnowledgeBaseChunk> Chunks { get; set; } = new List<KnowledgeBaseChunk>();
    }
}
