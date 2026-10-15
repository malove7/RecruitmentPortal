using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers
{
    [Authorize(Policy = "Permissions.AskKnowledgeBases")]
    public class KnowledgeBaseChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRagChatService _ragChatService;
        private readonly ILogger<KnowledgeBaseChatController> _logger;

        public KnowledgeBaseChatController(
            ApplicationDbContext context,
            IRagChatService ragChatService,
            ILogger<KnowledgeBaseChatController> logger)
        {
            _context = context;
            _ragChatService = ragChatService;
            _logger = logger;
        }

        // GET: KnowledgeBaseChat?knowledgeBaseId=1
        public async Task<IActionResult> Index(int? knowledgeBaseId)
        {
            var activeKbs = await _context.KnowledgeBases
                .Where(k => k.IsActive)
                .OrderBy(k => k.Name)
                .Select(k => new KnowledgeBaseItemViewModel
                {
                    Id = k.Id,
                    Name = k.Name,
                    Description = k.Description,
                    DocumentCount = k.Documents.Count,
                    IndexedDocumentCount = k.Documents.Count(d => d.Status == Models.Enums.KnowledgeBaseDocumentStatus.Indexed),
                    CreatedAt = k.CreatedAt
                })
                .ToListAsync();

            if (activeKbs.Count == 0)
            {
                ViewBag.NoActiveKb = true;
                return View(new KnowledgeBaseChatViewModel());
            }

            // If a specific knowledgeBaseId is provided, redirect directly to Session
            if (knowledgeBaseId.HasValue && activeKbs.Any(k => k.Id == knowledgeBaseId.Value))
            {
                return RedirectToAction(nameof(Session), new { knowledgeBaseId = knowledgeBaseId.Value });
            }

            // If only one KB exists, redirect to it directly for convenience
            if (activeKbs.Count == 1)
            {
                return RedirectToAction(nameof(Session), new { knowledgeBaseId = activeKbs[0].Id });
            }

            var model = new KnowledgeBaseChatViewModel
            {
                AvailableKnowledgeBases = activeKbs
            };

            return View(model);
        }

        // GET: KnowledgeBaseChat/Session?id=12&knowledgeBaseId=1
        public async Task<IActionResult> Session(int? id, int? knowledgeBaseId)
        {
            var activeKbs = await _context.KnowledgeBases
                .Where(k => k.IsActive)
                .OrderBy(k => k.Name)
                .Select(k => new KnowledgeBaseItemViewModel
                {
                    Id = k.Id,
                    Name = k.Name,
                    Description = k.Description,
                    DocumentCount = k.Documents.Count,
                    IndexedDocumentCount = k.Documents.Count(d => d.Status == Models.Enums.KnowledgeBaseDocumentStatus.Indexed)
                })
                .ToListAsync();

            if (activeKbs.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int selectedKbId = knowledgeBaseId ?? activeKbs.First().Id;
            var currentKb = await _context.KnowledgeBases.FirstOrDefaultAsync(k => k.Id == selectedKbId);
            if (currentKb == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch recent sessions for this KB (user's sessions or all sessions)
            var sessionsQuery = _context.KnowledgeBaseChatSessions
                .Where(s => s.KnowledgeBaseId == selectedKbId);

            if (!string.IsNullOrEmpty(userId))
            {
                sessionsQuery = sessionsQuery.Where(s => s.UserId == userId || s.UserId == null);
            }

            var sessions = await sessionsQuery
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new KnowledgeBaseChatSessionItemViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    UpdatedAt = s.UpdatedAt,
                    MessageCount = s.Messages.Count
                })
                .Take(20)
                .ToListAsync();

            KnowledgeBaseChatSession? currentSession = null;
            if (id.HasValue && id.Value > 0)
            {
                currentSession = await _context.KnowledgeBaseChatSessions
                    .Include(s => s.Messages)
                    .FirstOrDefaultAsync(s => s.Id == id.Value && s.KnowledgeBaseId == selectedKbId);
            }

            var messageViewModels = new List<KnowledgeBaseChatMessageViewModel>();
            if (currentSession != null)
            {
                foreach (var msg in currentSession.Messages.OrderBy(m => m.CreatedAt))
                {
                    var citations = new List<RagCitation>();
                    if (!string.IsNullOrWhiteSpace(msg.CitationsJson))
                    {
                        try
                        {
                            citations = JsonSerializer.Deserialize<List<RagCitation>>(msg.CitationsJson) ?? new();
                        }
                        catch
                        {
                            citations = new();
                        }
                    }

                    messageViewModels.Add(new KnowledgeBaseChatMessageViewModel
                    {
                        Id = msg.Id,
                        Role = msg.Role,
                        Content = msg.Content,
                        Citations = citations,
                        CreatedAt = msg.CreatedAt
                    });
                }
            }

            var model = new KnowledgeBaseChatViewModel
            {
                KnowledgeBaseId = selectedKbId,
                KnowledgeBaseName = currentKb.Name,
                AvailableKnowledgeBases = activeKbs,
                CurrentSessionId = currentSession?.Id,
                CurrentSessionTitle = currentSession?.Title ?? "New Chat",
                Sessions = sessions,
                Messages = messageViewModels
            };

            return View(model);
        }

        // POST: KnowledgeBaseChat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] AskQuestionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return Json(new AskQuestionResponse
                {
                    Success = false,
                    ErrorMessage = "Question cannot be empty."
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _ragChatService.AskQuestionAsync(
                request.KnowledgeBaseId,
                request.Question,
                request.SessionId,
                userId);

            return Json(new AskQuestionResponse
            {
                Success = result.IsSuccess,
                SessionId = result.SessionId,
                MessageId = result.MessageId,
                Answer = result.Answer,
                Citations = result.Citations,
                ErrorMessage = result.ErrorMessage
            });
        }

        // POST: KnowledgeBaseChat/NewSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewSession(int knowledgeBaseId)
        {
            return RedirectToAction(nameof(Session), new { knowledgeBaseId });
        }

        // POST: KnowledgeBaseChat/DeleteSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(int id, int knowledgeBaseId)
        {
            var session = await _context.KnowledgeBaseChatSessions.FindAsync(id);
            if (session != null)
            {
                _context.KnowledgeBaseChatSessions.Remove(session);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Chat session was deleted.";
            }

            return RedirectToAction(nameof(Session), new { knowledgeBaseId });
        }
    }
}
