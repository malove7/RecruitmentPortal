using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class AIResumeScreeningController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IResumeParserService _resumeParserService;
        private readonly IAIResumeScreeningService _aiScreeningService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AIResumeScreeningController> _logger;

        public AIResumeScreeningController(
            ApplicationDbContext context,
            IResumeParserService resumeParserService,
            IAIResumeScreeningService aiScreeningService,
            IWebHostEnvironment environment,
            ILogger<AIResumeScreeningController> logger)
        {
            _context = context;
            _resumeParserService = resumeParserService;
            _aiScreeningService = aiScreeningService;
            _environment = environment;
            _logger = logger;
        }

        // GET: /AIResumeScreening
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AIResumeScreeningCreateViewModel
            {
                JobPositions = await GetActiveJobPositionSelectListAsync()
            };
            return View(model);
        }

        // POST: /AIResumeScreening/Screen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Screen(AIResumeScreeningCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.JobPositions = await GetActiveJobPositionSelectListAsync();
                return View("Index", model);
            }

            // 1. Retrieve the Job Position from the database
            var jobPosition = await _context.JobPositions
                .FirstOrDefaultAsync(j => j.Id == model.JobPositionId);

            if (jobPosition == null)
            {
                ModelState.AddModelError(nameof(model.JobPositionId), "The selected Job Position was not found.");
                model.JobPositions = await GetActiveJobPositionSelectListAsync();
                return View("Index", model);
            }

            // 2. Validate and Parse the uploaded resume file
            var parseResult = await _resumeParserService.ParseResumeAsync(model.ResumeFile);
            if (!parseResult.IsSuccess)
            {
                ModelState.AddModelError(nameof(model.ResumeFile), parseResult.ErrorMessage ?? "Failed to parse the uploaded resume file.");
                model.JobPositions = await GetActiveJobPositionSelectListAsync();
                return View("Index", model);
            }

            // 3. Save resume file to wwwroot/uploads/resumes
            string relativeFilePath;
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var safeFileName = Path.GetFileName(model.ResumeFile!.FileName);
                var uniqueFileName = $"{Guid.NewGuid():N}_{safeFileName}";
                var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await model.ResumeFile.CopyToAsync(stream);
                }

                relativeFilePath = $"/uploads/resumes/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save uploaded resume file to disk.");
                ModelState.AddModelError(string.Empty, "Unable to save the uploaded resume file on server.");
                model.JobPositions = await GetActiveJobPositionSelectListAsync();
                return View("Index", model);
            }

            // 4. Send Job Position + Resume to OpenAI
            var aiInput = new AIScreeningInput
            {
                JobPositionId = jobPosition.Id,
                JobTitle = jobPosition.Title,
                JobDescription = jobPosition.Description,
                JobRequirements = jobPosition.Requirements ?? string.Empty,
                ResumeFileName = model.ResumeFile!.FileName,
                ResumeText = parseResult.ExtractedText,
                Model = !string.IsNullOrWhiteSpace(model.SelectedModel) ? model.SelectedModel : null
            };

            var aiResult = await _aiScreeningService.ScreenResumeAsync(aiInput);
            if (!aiResult.IsSuccess || aiResult.Data == null)
            {
                _logger.LogWarning("AI resume screening failed: {Error}", aiResult.ErrorMessage);
                ModelState.AddModelError(string.Empty, aiResult.ErrorMessage ?? "AI screening service returned an error. Please try again.");
                model.JobPositions = await GetActiveJobPositionSelectListAsync();
                return View("Index", model);
            }

            var aiData = aiResult.Data;
            var currentUser = User.Identity?.Name ?? "HR User";

            // 5. Persist screening results to database
            var screening = new AIResumeScreening
            {
                JobPositionId = jobPosition.Id,
                ResumeFileName = model.ResumeFile.FileName,
                ResumeFilePath = relativeFilePath,
                OverallScore = Math.Clamp(aiData.OverallScore, 0, 100),
                Recommendation = aiData.Recommendation ?? string.Empty,
                RecruiterSummary = aiData.RecruiterSummary ?? string.Empty,
                JobTitleSnapshot = jobPosition.Title,
                JobDescriptionSnapshot = jobPosition.Description,
                JobRequirementsSnapshot = jobPosition.Requirements ?? string.Empty,
                RequiredExperience = aiData.ExperienceAnalysis?.RequiredExperience,
                CandidateExperience = aiData.ExperienceAnalysis?.CandidateExperience,
                ExperienceStatus = aiData.ExperienceAnalysis?.Status,
                ExperienceComments = aiData.ExperienceAnalysis?.Comments,
                AIModel = aiResult.ModelUsed,
                PromptVersion = aiResult.PromptVersion,
                CreatedBy = currentUser,
                CreatedDate = DateTime.UtcNow,
                RawJsonResponse = aiResult.RawJsonResponse
            };

            // Map Skills
            if (aiData.Skills != null)
            {
                foreach (var s in aiData.Skills)
                {
                    screening.Skills.Add(new AIResumeSkillEvaluation
                    {
                        Skill = s.Skill ?? string.Empty,
                        Score = Math.Clamp(s.Score, 0, 10),
                        MaxScore = s.MaxScore > 0 ? s.MaxScore : 10,
                        Status = s.Status ?? string.Empty,
                        Confidence = s.Confidence,
                        EvidenceJson = s.Evidence != null ? JsonSerializer.Serialize(s.Evidence) : null,
                        GapsJson = s.Gaps != null ? JsonSerializer.Serialize(s.Gaps) : null
                    });
                }
            }

            // Map Strengths
            if (aiData.Strengths != null)
            {
                foreach (var st in aiData.Strengths.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    screening.Strengths.Add(new AIResumeStrength { Strength = st });
                }
            }

            // Map Evidence Extracted
            if (aiData.EvidenceExtracted != null)
            {
                foreach (var ev in aiData.EvidenceExtracted.Where(x => !string.IsNullOrWhiteSpace(x.Evidence)))
                {
                    screening.EvidenceExtracted.Add(new AIResumeEvidence
                    {
                        Category = ev.Category ?? "General",
                        Evidence = ev.Evidence
                    });
                }
            }

            // Map Concerns
            if (aiData.Concerns != null)
            {
                foreach (var c in aiData.Concerns.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    screening.Concerns.Add(new AIResumeConcern { Concern = c });
                }
            }

            // Map Gaps
            if (aiData.Gaps != null)
            {
                foreach (var g in aiData.Gaps.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    screening.Gaps.Add(new AIResumeGap { Gap = g });
                }
            }

            // Map Follow-up Questions
            if (aiData.FollowUpQuestions != null)
            {
                foreach (var q in aiData.FollowUpQuestions.Where(x => !string.IsNullOrWhiteSpace(x.Question)))
                {
                    screening.FollowUpQuestions.Add(new AIResumeFollowUpQuestion
                    {
                        Category = q.Category ?? "Technical",
                        Skill = q.Skill ?? string.Empty,
                        Priority = NormalizePriority(q.Priority),
                        Reason = q.Reason ?? string.Empty,
                        Question = q.Question
                    });
                }
            }

            _context.AIResumeScreenings.Add(screening);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved AI resume screening ID {Id} for job position {JobPositionId}.", screening.Id, jobPosition.Id);

            return RedirectToAction(nameof(Result), new { id = screening.Id });
        }

        // GET: /AIResumeScreening/Result/{id}
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var screening = await _context.AIResumeScreenings
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.JobPosition)
                .Include(s => s.Skills)
                .Include(s => s.Strengths)
                .Include(s => s.EvidenceExtracted)
                .Include(s => s.Concerns)
                .Include(s => s.Gaps)
                .Include(s => s.FollowUpQuestions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (screening == null)
            {
                return NotFound();
            }

            var viewModel = new AIResumeScreeningResultViewModel
            {
                Id = screening.Id,
                JobPositionId = screening.JobPositionId,
                JobPositionTitle = screening.JobPosition?.Title ?? screening.JobTitleSnapshot,
                JobDescriptionSnapshot = screening.JobDescriptionSnapshot,
                JobRequirementsSnapshot = screening.JobRequirementsSnapshot,
                ResumeFileName = screening.ResumeFileName,
                ResumeFilePath = screening.ResumeFilePath,
                OverallScore = screening.OverallScore,
                Recommendation = screening.Recommendation,
                RecruiterSummary = screening.RecruiterSummary,
                ScreeningDate = screening.CreatedDate,
                ScreenedBy = screening.CreatedBy,
                AIModel = screening.AIModel,
                PromptVersion = screening.PromptVersion,
                ExperienceAnalysis = new AIExperienceAnalysisDto
                {
                    RequiredExperience = screening.RequiredExperience ?? string.Empty,
                    CandidateExperience = screening.CandidateExperience ?? string.Empty,
                    Status = screening.ExperienceStatus ?? string.Empty,
                    Comments = screening.ExperienceComments ?? string.Empty
                },
                Strengths = screening.Strengths.Select(st => st.Strength).ToList(),
                EvidenceExtracted = screening.EvidenceExtracted.Select(e => new AIEvidenceDto
                {
                    Category = e.Category,
                    Evidence = e.Evidence
                }).ToList(),
                Concerns = screening.Concerns.Select(c => c.Concern).ToList(),
                Gaps = screening.Gaps.Select(g => g.Gap).ToList(),
                Skills = screening.Skills.Select(sk => new AIResumeSkillEvaluationItemViewModel
                {
                    Skill = sk.Skill,
                    Score = sk.Score,
                    MaxScore = sk.MaxScore,
                    Status = sk.Status,
                    Confidence = sk.Confidence,
                    Evidence = DeserializeStringList(sk.EvidenceJson),
                    Gaps = DeserializeStringList(sk.GapsJson)
                }).OrderByDescending(s => s.Score).ToList(),
                FollowUpQuestions = screening.FollowUpQuestions
                    .Select(q => new AIFollowUpQuestionDto
                    {
                        Category = q.Category,
                        Skill = q.Skill,
                        Priority = q.Priority,
                        Reason = q.Reason,
                        Question = q.Question
                    })
                    .OrderBy(q => PriorityOrder(q.Priority))
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: /AIResumeScreening/History
        [HttpGet]
        public async Task<IActionResult> History(int? jobPositionId, string? searchTerm)
        {
            var query = _context.AIResumeScreenings
                .AsNoTracking()
                .AsQueryable();

            if (jobPositionId.HasValue && jobPositionId.Value > 0)
            {
                query = query.Where(s => s.JobPositionId == jobPositionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => (s.ResumeFileName != null && s.ResumeFileName.ToLower().Contains(term)) ||
                                         (s.Recommendation != null && s.Recommendation.ToLower().Contains(term)) ||
                                         (s.JobTitleSnapshot != null && s.JobTitleSnapshot.ToLower().Contains(term)) ||
                                         (s.CreatedBy != null && s.CreatedBy.ToLower().Contains(term)));
            }

            var screenings = await query
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new AIResumeScreeningHistoryItemViewModel
                {
                    Id = s.Id,
                    ResumeFileName = s.ResumeFileName,
                    ResumeFilePath = s.ResumeFilePath,
                    JobPositionId = s.JobPositionId,
                    JobPositionTitle = s.JobPosition != null ? s.JobPosition.Title : s.JobTitleSnapshot,
                    OverallScore = s.OverallScore,
                    Recommendation = s.Recommendation ?? string.Empty,
                    ScreenedBy = s.CreatedBy ?? string.Empty,
                    Date = s.CreatedDate
                })
                .ToListAsync();

            var viewModel = new AIResumeScreeningHistoryViewModel
            {
                Screenings = screenings,
                SelectedJobPositionId = jobPositionId,
                SearchTerm = searchTerm,
                JobPositions = await GetActiveJobPositionSelectListAsync(includeAllOption: true)
            };

            return View(viewModel);
        }

        // GET: /AIResumeScreening/GetJobPositionDetails/5 (for AJAX interactive preview)
        [HttpGet]
        public async Task<IActionResult> GetJobPositionDetails(int id)
        {
            var jobPosition = await _context.JobPositions
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jobPosition == null)
            {
                return Json(new { success = false, message = "Position not found." });
            }

            return Json(new
            {
                success = true,
                title = jobPosition.Title,
                description = jobPosition.Description,
                requirements = jobPosition.Requirements ?? string.Empty
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetActiveJobPositionSelectListAsync(bool includeAllOption = false)
        {
            var positions = await _context.JobPositions
                .AsNoTracking()
                .Where(j => j.IsActive)
                .OrderBy(j => j.Title)
                .Select(j => new SelectListItem
                {
                    Value = j.Id.ToString(),
                    Text = j.Title
                })
                .ToListAsync();

            if (includeAllOption)
            {
                positions.Insert(0, new SelectListItem { Value = "", Text = "All Job Positions" });
            }

            return positions;
        }

        private static int PriorityOrder(string priority) =>
            priority?.Trim().ToLowerInvariant() switch
            {
                "high" => 1,
                "medium" => 2,
                "low" => 3,
                _ => 4
            };

        private static string NormalizePriority(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority)) return "Medium";
            var p = priority.Trim().ToLowerInvariant();
            if (p.Contains("high")) return "High";
            if (p.Contains("low")) return "Low";
            return "Medium";
        }

        private static List<string> DeserializeStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string> { json };
            }
        }
    }
}
