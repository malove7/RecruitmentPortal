using System.ComponentModel.DataAnnotations;
using RecruitmentPortal.Models.Enums;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Models.ViewModels
{
    public class KnowledgeBaseItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DocumentCount { get; set; }
        public int IndexedDocumentCount { get; set; }
        public int FailedDocumentCount { get; set; }
        public int TotalChunks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class KnowledgeBaseListViewModel
    {
        public List<KnowledgeBaseItemViewModel> KnowledgeBases { get; set; } = new();
        public int TotalKnowledgeBases => KnowledgeBases.Count;
        public int TotalDocuments => KnowledgeBases.Sum(k => k.DocumentCount);
        public int TotalIndexedDocuments => KnowledgeBases.Sum(k => k.IndexedDocumentCount);
        public int TotalChunks => KnowledgeBases.Sum(k => k.TotalChunks);
    }

    public class KnowledgeBaseCreateViewModel
    {
        [Required(ErrorMessage = "Knowledge Base name is required")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
        [Display(Name = "Knowledge Base Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class KnowledgeBaseEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Knowledge Base name is required")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
        [Display(Name = "Knowledge Base Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }

    public class KnowledgeBaseDocumentItemViewModel
    {
        public int Id { get; set; }
        public int KnowledgeBaseId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public KnowledgeBaseDocumentStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ExtractedCharCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? IndexedAt { get; set; }
        public string? UploadedByName { get; set; }

        public string FormattedFileSize
        {
            get
            {
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
                return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
            }
        }
    }

    public class KnowledgeBaseDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public List<KnowledgeBaseDocumentItemViewModel> Documents { get; set; } = new();
        public int TotalChunks => Documents.Sum(d => d.ChunkCount);
        public int IndexedCount => Documents.Count(d => d.Status == KnowledgeBaseDocumentStatus.Indexed);
        public int FailedCount => Documents.Count(d => d.Status == KnowledgeBaseDocumentStatus.Failed);
    }

    public class KnowledgeBaseDocumentsViewModel
    {
        public int KnowledgeBaseId { get; set; }
        public string KnowledgeBaseName { get; set; } = string.Empty;
        public int MaxFileSizeMB { get; set; } = 25;
        public List<KnowledgeBaseDocumentItemViewModel> Documents { get; set; } = new();
    }

    public class KnowledgeBaseChatSessionItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class KnowledgeBaseChatMessageViewModel
    {
        public int Id { get; set; }
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public List<RagCitation> Citations { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class KnowledgeBaseChatViewModel
    {
        public int KnowledgeBaseId { get; set; }
        public string KnowledgeBaseName { get; set; } = string.Empty;
        public List<KnowledgeBaseItemViewModel> AvailableKnowledgeBases { get; set; } = new();
        public int? CurrentSessionId { get; set; }
        public string CurrentSessionTitle { get; set; } = "New Chat";
        public List<KnowledgeBaseChatSessionItemViewModel> Sessions { get; set; } = new();
        public List<KnowledgeBaseChatMessageViewModel> Messages { get; set; } = new();
    }

    public class AskQuestionRequest
    {
        public int KnowledgeBaseId { get; set; }
        public int? SessionId { get; set; }
        public string Question { get; set; } = string.Empty;
    }

    public class AskQuestionResponse
    {
        public bool Success { get; set; }
        public int SessionId { get; set; }
        public int MessageId { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<RagCitation> Citations { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}
