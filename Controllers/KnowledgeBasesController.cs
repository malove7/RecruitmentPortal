using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers
{
    [Authorize(Policy = "Permissions.ViewKnowledgeBases")]
    public class KnowledgeBasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IKnowledgeBaseIngestionService _ingestionService;
        private readonly KnowledgeBaseOptions _kbOptions;
        private readonly ILogger<KnowledgeBasesController> _logger;

        public KnowledgeBasesController(
            ApplicationDbContext context,
            IKnowledgeBaseIngestionService ingestionService,
            IOptions<KnowledgeBaseOptions> kbOptions,
            ILogger<KnowledgeBasesController> logger)
        {
            _context = context;
            _ingestionService = ingestionService;
            _kbOptions = kbOptions.Value;
            _logger = logger;
        }

        // GET: KnowledgeBases
        public async Task<IActionResult> Index()
        {
            var kbs = await _context.KnowledgeBases
                .Include(k => k.Documents)
                    .ThenInclude(d => d.Chunks)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();

            var viewModel = new KnowledgeBaseListViewModel
            {
                KnowledgeBases = kbs.Select(k => new KnowledgeBaseItemViewModel
                {
                    Id = k.Id,
                    Name = k.Name,
                    Description = k.Description,
                    DocumentCount = k.Documents.Count,
                    IndexedDocumentCount = k.Documents.Count(d => d.Status == Models.Enums.KnowledgeBaseDocumentStatus.Indexed),
                    FailedDocumentCount = k.Documents.Count(d => d.Status == Models.Enums.KnowledgeBaseDocumentStatus.Failed),
                    TotalChunks = k.Documents.SelectMany(d => d.Chunks).Count(),
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt,
                    IsActive = k.IsActive
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: KnowledgeBases/Create
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public IActionResult Create()
        {
            return View(new KnowledgeBaseCreateViewModel());
        }

        // POST: KnowledgeBases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> Create(KnowledgeBaseCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var kb = new KnowledgeBase
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            _context.KnowledgeBases.Add(kb);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Knowledge Base '{kb.Name}' was created successfully.";
            return RedirectToAction(nameof(Documents), new { id = kb.Id });
        }

        // GET: KnowledgeBases/Edit/5
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> Edit(int id)
        {
            var kb = await _context.KnowledgeBases.FindAsync(id);
            if (kb == null)
            {
                return NotFound();
            }

            var model = new KnowledgeBaseEditViewModel
            {
                Id = kb.Id,
                Name = kb.Name,
                Description = kb.Description,
                IsActive = kb.IsActive
            };

            return View(model);
        }

        // POST: KnowledgeBases/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> Edit(int id, KnowledgeBaseEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var kb = await _context.KnowledgeBases.FindAsync(id);
            if (kb == null)
            {
                return NotFound();
            }

            kb.Name = model.Name.Trim();
            kb.Description = model.Description?.Trim();
            kb.IsActive = model.IsActive;
            kb.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Knowledge Base '{kb.Name}' was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: KnowledgeBases/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var kb = await _context.KnowledgeBases
                .Include(k => k.CreatedByUser)
                .Include(k => k.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kb == null)
            {
                return NotFound();
            }

            var model = new KnowledgeBaseDetailsViewModel
            {
                Id = kb.Id,
                Name = kb.Name,
                Description = kb.Description,
                IsActive = kb.IsActive,
                CreatedAt = kb.CreatedAt,
                UpdatedAt = kb.UpdatedAt,
                CreatedByName = kb.CreatedByUser != null ? $"{kb.CreatedByUser.FirstName} {kb.CreatedByUser.LastName}" : "System",
                Documents = kb.Documents.OrderByDescending(d => d.UploadedAt).Select(d => new KnowledgeBaseDocumentItemViewModel
                {
                    Id = d.Id,
                    KnowledgeBaseId = d.KnowledgeBaseId,
                    FileName = d.FileName,
                    FileExtension = d.FileExtension,
                    FileSizeBytes = d.FileSizeBytes,
                    Status = d.Status,
                    ErrorMessage = d.ErrorMessage,
                    ExtractedCharCount = d.ExtractedCharCount,
                    ChunkCount = d.ChunkCount,
                    UploadedAt = d.UploadedAt,
                    IndexedAt = d.IndexedAt,
                    UploadedByName = d.UploadedByUser != null ? $"{d.UploadedByUser.FirstName} {d.UploadedByUser.LastName}" : null
                }).ToList()
            };

            return View(model);
        }

        // GET: KnowledgeBases/Documents/5
        public async Task<IActionResult> Documents(int id)
        {
            var kb = await _context.KnowledgeBases
                .Include(k => k.Documents)
                    .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kb == null)
            {
                return NotFound();
            }

            var model = new KnowledgeBaseDocumentsViewModel
            {
                KnowledgeBaseId = kb.Id,
                KnowledgeBaseName = kb.Name,
                MaxFileSizeMB = _kbOptions.MaxFileSizeMB > 0 ? _kbOptions.MaxFileSizeMB : 25,
                Documents = kb.Documents.OrderByDescending(d => d.UploadedAt).Select(d => new KnowledgeBaseDocumentItemViewModel
                {
                    Id = d.Id,
                    KnowledgeBaseId = d.KnowledgeBaseId,
                    FileName = d.FileName,
                    FileExtension = d.FileExtension,
                    FileSizeBytes = d.FileSizeBytes,
                    Status = d.Status,
                    ErrorMessage = d.ErrorMessage,
                    ExtractedCharCount = d.ExtractedCharCount,
                    ChunkCount = d.ChunkCount,
                    UploadedAt = d.UploadedAt,
                    IndexedAt = d.IndexedAt,
                    UploadedByName = d.UploadedByUser != null ? $"{d.UploadedByUser.FirstName} {d.UploadedByUser.LastName}" : null
                }).ToList()
            };

            return View(model);
        }

        // POST: KnowledgeBases/UploadDocuments/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> UploadDocuments(int id, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one PDF or DOCX file to upload.";
                return RedirectToAction(nameof(Documents), new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int successCount = 0;
            var failureMessages = new List<string>();

            foreach (var file in files)
            {
                var result = await _ingestionService.IngestDocumentAsync(id, file, userId);
                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failureMessages.Add($"'{result.FileName}': {result.ErrorMessage}");
                }
            }

            if (successCount > 0 && failureMessages.Count == 0)
            {
                TempData["SuccessMessage"] = $"Successfully uploaded and indexed {successCount} document(s).";
            }
            else if (successCount > 0 && failureMessages.Count > 0)
            {
                TempData["WarningMessage"] = $"Indexed {successCount} document(s), but {failureMessages.Count} failed: {string.Join("; ", failureMessages)}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to process documents: {string.Join("; ", failureMessages)}";
            }

            return RedirectToAction(nameof(Documents), new { id });
        }

        // POST: KnowledgeBases/ReindexDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> ReindexDocument(int id, int documentId)
        {
            var result = await _ingestionService.ReindexDocumentAsync(documentId);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Document '{result.FileName}' was successfully reindexed ({result.ChunkCount} chunks).";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to reindex document '{result.FileName}': {result.ErrorMessage}";
            }

            return RedirectToAction(nameof(Documents), new { id });
        }

        // POST: KnowledgeBases/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> DeleteDocument(int id, int documentId)
        {
            await _ingestionService.DeleteDocumentAsync(documentId);
            TempData["SuccessMessage"] = "Document was deleted from knowledge base, local storage, and Pinecone index.";
            return RedirectToAction(nameof(Documents), new { id });
        }

        // POST: KnowledgeBases/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permissions.ManageKnowledgeBases")]
        public async Task<IActionResult> Delete(int id)
        {
            var kb = await _context.KnowledgeBases.FindAsync(id);
            if (kb == null)
            {
                return NotFound();
            }

            var kbName = kb.Name;
            await _ingestionService.DeleteKnowledgeBaseAsync(id);

            TempData["SuccessMessage"] = $"Knowledge Base '{kbName}' and its index were successfully deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
