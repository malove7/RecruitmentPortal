using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.ViewModels;

namespace RecruitmentPortal.Controllers
{
    public class CandidateEvaluationFormsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidateEvaluationFormsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Public: submit form ──────────────────────────────────────────────

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CandidateEvaluationFormViewModel
            {
                SubmissionToken = Guid.NewGuid()
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CandidateEvaluationFormViewModel model)
        {
            if (model.SubmissionToken == Guid.Empty)
                ModelState.AddModelError(string.Empty, "This form has expired. Please reload the page and try again.");

            if (!ModelState.IsValid)
                return View(model);

            if (await _context.CandidateEvaluationForms
                .AnyAsync(f => f.SubmissionToken == model.SubmissionToken))
            {
                return RedirectToAction(nameof(Success));
            }

            var form = MapFromViewModel(model);
            form.SubmittedAt = DateTime.UtcNow;

            _context.CandidateEvaluationForms.Add(form);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(form).State = EntityState.Detached;

                if (await _context.CandidateEvaluationForms
                    .AnyAsync(f => f.SubmissionToken == model.SubmissionToken))
                {
                    return RedirectToAction(nameof(Success));
                }

                throw;
            }

            return RedirectToAction(nameof(Success));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        // ── Public: Interviewer feedback link & submission ───────────────────

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Feedback(string? token)
        {
            var model = new InterviewerFeedbackViewModel();

            if (string.IsNullOrWhiteSpace(token))
            {
                model.IsValid = false;
                model.ErrorMessage = "The feedback link is invalid or missing.";
                return View(model);
            }

            var tokenEntity = await _context.CandidateFeedbackTokens
                .Include(t => t.CandidateEvaluationForm)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenEntity == null)
            {
                model.IsValid = false;
                model.ErrorMessage = "The feedback link is invalid or has expired.";
                return View(model);
            }

            if (tokenEntity.IsUsed)
            {
                model.IsValid = false;
                model.ErrorMessage = "This feedback link is no longer available because feedback has already been submitted.";
                return View(model);
            }

            if (tokenEntity.CandidateEvaluationForm == null)
            {
                model.IsValid = false;
                model.ErrorMessage = "The candidate evaluation record associated with this link could not be found.";
                return View(model);
            }

            var form = tokenEntity.CandidateEvaluationForm;
            model.Token = token;
            model.FormId = form.Id;
            model.CandidateFullName = form.FullName;
            model.CandidateEmail = form.Email;
            model.PositionAppliedFor = form.PositionAppliedFor;
            model.SelectedFeedbackType = tokenEntity.FeedbackType;
            model.IsTypePreSelected = !string.IsNullOrWhiteSpace(tokenEntity.FeedbackType);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(InterviewerFeedbackViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Token))
            {
                model.IsValid = false;
                model.ErrorMessage = "The feedback link is invalid or missing.";
                return View(model);
            }

            var tokenEntity = await _context.CandidateFeedbackTokens
                .Include(t => t.CandidateEvaluationForm)
                .FirstOrDefaultAsync(t => t.Token == model.Token);

            if (tokenEntity == null)
            {
                model.IsValid = false;
                model.ErrorMessage = "The feedback link is invalid or has expired.";
                return View(model);
            }

            if (tokenEntity.IsUsed)
            {
                model.IsValid = false;
                model.ErrorMessage = "This feedback link is no longer available because feedback has already been submitted.";
                return View(model);
            }

            if (tokenEntity.CandidateEvaluationForm == null)
            {
                model.IsValid = false;
                model.ErrorMessage = "The candidate evaluation record associated with this link could not be found.";
                return View(model);
            }

            var form = tokenEntity.CandidateEvaluationForm;
            model.CandidateFullName = form.FullName;
            model.CandidateEmail = form.Email;
            model.PositionAppliedFor = form.PositionAppliedFor;
            model.FormId = form.Id;
            model.IsTypePreSelected = !string.IsNullOrWhiteSpace(tokenEntity.FeedbackType);

            if (model.IsTypePreSelected)
            {
                model.SelectedFeedbackType = tokenEntity.FeedbackType;
            }

            if (string.IsNullOrWhiteSpace(model.SelectedFeedbackType) ||
                (model.SelectedFeedbackType != "Technical" && model.SelectedFeedbackType != "Practical"))
            {
                ModelState.AddModelError(nameof(model.SelectedFeedbackType), "Please select a feedback type (Technical or Practical).");
            }

            if (string.IsNullOrWhiteSpace(model.ReviewerSignature))
            {
                ModelState.AddModelError(nameof(model.ReviewerSignature), "Interviewer name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Comments))
            {
                ModelState.AddModelError(nameof(model.Comments), "Feedback comments are required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SelectedFeedbackType == "Technical")
            {
                form.TechnicalComments = model.Comments.Trim();
                form.TechnicalReviewerSignature = model.ReviewerSignature.Trim();
            }
            else // Practical
            {
                var signature = model.ReviewerSignature.Trim();
                var comments = model.Comments.Trim();
                var formatted = $"Reviewer: {signature}\nComments: {comments}";
                if (formatted.Length > 1000)
                {
                    formatted = formatted.Substring(0, 1000);
                }
                form.OtherComments = formatted;
            }

            tokenEntity.FeedbackType ??= model.SelectedFeedbackType;
            tokenEntity.IsUsed = true;
            tokenEntity.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(FeedbackSuccess), new { token = model.Token });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> FeedbackSuccess(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Create));
            }

            var tokenEntity = await _context.CandidateFeedbackTokens
                .Include(t => t.CandidateEvaluationForm)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenEntity == null || tokenEntity.CandidateEvaluationForm == null)
            {
                return RedirectToAction(nameof(Create));
            }

            var model = new FeedbackSuccessViewModel
            {
                CandidateFullName = tokenEntity.CandidateEvaluationForm.FullName,
                PositionAppliedFor = tokenEntity.CandidateEvaluationForm.PositionAppliedFor,
                FeedbackType = tokenEntity.FeedbackType ?? "Candidate",
                SubmittedAt = tokenEntity.UsedAt ?? DateTime.UtcNow
            };

            return View(model);
        }

        // ── Authenticated: list / view / edit / delete / generate link ────────

        [Authorize(Policy = "Permissions.ViewEvaluationForms")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateFeedbackLink([FromBody] GenerateFeedbackLinkRequest request)
        {
            if (request == null || request.FormId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid candidate evaluation form ID." });
            }

            var form = await _context.CandidateEvaluationForms.FindAsync(request.FormId);
            if (form == null)
            {
                return NotFound(new { success = false, message = "Candidate evaluation form not found." });
            }

            string? feedbackType = null;
            if (!string.IsNullOrWhiteSpace(request.FeedbackType))
            {
                var trimmed = request.FeedbackType.Trim();
                if (string.Equals(trimmed, "Technical", StringComparison.OrdinalIgnoreCase))
                {
                    feedbackType = "Technical";
                }
                else if (string.Equals(trimmed, "Practical", StringComparison.OrdinalIgnoreCase))
                {
                    feedbackType = "Practical";
                }
            }

            var token = Guid.NewGuid().ToString("N");
            var feedbackToken = new CandidateFeedbackToken
            {
                CandidateEvaluationFormId = form.Id,
                Token = token,
                FeedbackType = feedbackType,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.CandidateFeedbackTokens.Add(feedbackToken);
            await _context.SaveChangesAsync();

            var scheme = Request?.Scheme ?? "https";
            var url = Url?.Action(nameof(Feedback), "CandidateEvaluationForms", new { token }, scheme)
                      ?? $"/CandidateEvaluationForms/Feedback?token={token}";
            return Ok(new { success = true, url });
        }

        [Authorize(Policy = "Permissions.ViewEvaluationForms")]
        public async Task<IActionResult> Index(string? search, EvaluationStatus? status)
        {
            var query = _context.CandidateEvaluationForms.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(f =>
                    f.FullName.Contains(search) ||
                    f.Email.Contains(search) ||
                    f.PositionAppliedFor.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(f => f.EvaluationStatus == status.Value);
            }

            ViewData["CurrentFilter"] = search;
            ViewData["CurrentStatus"] = status;
            return View(await query.OrderByDescending(f => f.SubmittedAt).ToListAsync());
        }

        [Authorize(Policy = "Permissions.ViewEvaluationForms")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var form = await _context.CandidateEvaluationForms
                .Include(f => f.WorkExperiences)
                .Include(f => f.EducationRecords)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null) return NotFound();

            return View(form);
        }

        [Authorize(Policy = "Permissions.EditEvaluationForms")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var form = await _context.CandidateEvaluationForms
                .Include(f => f.WorkExperiences)
                .Include(f => f.EducationRecords)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null) return NotFound();

            return View(MapToViewModel(form));
        }

        [Authorize(Policy = "Permissions.EditEvaluationForms")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CandidateEvaluationFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var form = await _context.CandidateEvaluationForms
                .Include(f => f.WorkExperiences)
                .Include(f => f.EducationRecords)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null) return NotFound();

            // Update scalar fields
            form.Email = model.Email;
            form.InterviewDate = model.InterviewDate;
            form.InterviewTime = model.InterviewTime;
            form.FullName = model.FullName;
            form.PositionAppliedFor = model.PositionAppliedFor;
            form.TotalExperience = model.TotalExperience;
            form.RelevantIndustryExperience = model.RelevantIndustryExperience;
            form.DateOfBirth = model.DateOfBirth;
            form.ContactNumber = model.ContactNumber;
            form.ResidenceAddress = model.ResidenceAddress;
            form.Married = model.Married;
            form.Child = model.Child;
            form.CurrentOrganization = model.CurrentOrganization;
            form.ExpectedCTC = model.ExpectedCTC;
            form.OfferedCTC = model.OfferedCTC;
            form.JoiningDate = model.JoiningDate;
            form.NoticePeriod = model.NoticePeriod;
            form.PreviouslyInterviewed = model.PreviouslyInterviewed;
            form.Declaration = model.Declaration;
            form.DigitalSignature = model.DigitalSignature;
            form.CommunicationSkills = model.CommunicationSkills;
            form.Confidence = model.Confidence;
            form.HRComments = model.HRComments;
            form.HRSignature = model.HRSignature;
            form.EvaluationStatus = model.EvaluationStatus;
            form.TechnicalComments = model.TechnicalComments;
            form.TechnicalReviewerSignature = model.TechnicalReviewerSignature;
            form.OtherComments = model.OtherComments;

            // Replace child records
            _context.WorkExperienceRecords.RemoveRange(form.WorkExperiences);
            _context.EducationRecords.RemoveRange(form.EducationRecords);

            foreach (var we in model.WorkExperiences)
            {
                if (!string.IsNullOrWhiteSpace(we.CompanyName) || !string.IsNullOrWhiteSpace(we.Designation))
                {
                    form.WorkExperiences.Add(new WorkExperienceRecord
                    {
                        RecordNumber = we.RecordNumber,
                        CompanyName = we.CompanyName,
                        Designation = we.Designation,
                        StartDate = we.StartDate,
                        EndDate = we.EndDate,
                        DurationOfWork = we.DurationOfWork,
                        LastCTCPerAnnum = we.LastCTCPerAnnum
                    });
                }
            }

            foreach (var edu in model.EducationRecords)
            {
                if (!string.IsNullOrWhiteSpace(edu.DegreeCourse))
                {
                    form.EducationRecords.Add(new EducationRecord
                    {
                        RecordNumber = edu.RecordNumber,
                        DegreeCourse = edu.DegreeCourse,
                        YearOfPassing = edu.YearOfPassing,
                        DivisionPercentage = edu.DivisionPercentage
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Permissions.DeleteEvaluationForms")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var form = await _context.CandidateEvaluationForms
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null) return NotFound();

            return View(form);
        }

        [Authorize(Policy = "Permissions.DeleteEvaluationForms")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var form = await _context.CandidateEvaluationForms.FindAsync(id);
            if (form != null)
                _context.CandidateEvaluationForms.Remove(form);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static CandidateEvaluationForm MapFromViewModel(CandidateEvaluationFormViewModel model)
        {
            var form = new CandidateEvaluationForm
            {
                SubmissionToken = model.SubmissionToken,
                Email = model.Email,
                InterviewDate = model.InterviewDate,
                InterviewTime = model.InterviewTime,
                FullName = model.FullName,
                PositionAppliedFor = model.PositionAppliedFor,
                TotalExperience = model.TotalExperience,
                RelevantIndustryExperience = model.RelevantIndustryExperience,
                DateOfBirth = model.DateOfBirth,
                ContactNumber = model.ContactNumber,
                ResidenceAddress = model.ResidenceAddress,
                Married = model.Married,
                Child = model.Child,
                CurrentOrganization = model.CurrentOrganization,
                ExpectedCTC = model.ExpectedCTC,
                NoticePeriod = model.NoticePeriod,
                PreviouslyInterviewed = model.PreviouslyInterviewed,
                Declaration = model.Declaration,
                DigitalSignature = model.DigitalSignature,
                CommunicationSkills = model.CommunicationSkills,
                Confidence = model.Confidence,
                HRComments = model.HRComments,
                HRSignature = model.HRSignature,
                TechnicalComments = model.TechnicalComments,
                TechnicalReviewerSignature = model.TechnicalReviewerSignature,
                OtherComments = model.OtherComments
            };

            foreach (var we in model.WorkExperiences)
            {
                if (!string.IsNullOrWhiteSpace(we.CompanyName) || !string.IsNullOrWhiteSpace(we.Designation))
                {
                    form.WorkExperiences.Add(new WorkExperienceRecord
                    {
                        RecordNumber = we.RecordNumber,
                        CompanyName = we.CompanyName,
                        Designation = we.Designation,
                        StartDate = we.StartDate,
                        EndDate = we.EndDate,
                        DurationOfWork = we.DurationOfWork,
                        LastCTCPerAnnum = we.LastCTCPerAnnum
                    });
                }
            }

            foreach (var edu in model.EducationRecords)
            {
                if (!string.IsNullOrWhiteSpace(edu.DegreeCourse))
                {
                    form.EducationRecords.Add(new EducationRecord
                    {
                        RecordNumber = edu.RecordNumber,
                        DegreeCourse = edu.DegreeCourse,
                        YearOfPassing = edu.YearOfPassing,
                        DivisionPercentage = edu.DivisionPercentage
                    });
                }
            }

            return form;
        }

        private static CandidateEvaluationFormViewModel MapToViewModel(CandidateEvaluationForm form)
        {
            var vm = new CandidateEvaluationFormViewModel
            {
                Id = form.Id,
                Email = form.Email,
                InterviewDate = form.InterviewDate,
                InterviewTime = form.InterviewTime,
                FullName = form.FullName,
                PositionAppliedFor = form.PositionAppliedFor,
                TotalExperience = form.TotalExperience,
                RelevantIndustryExperience = form.RelevantIndustryExperience,
                DateOfBirth = form.DateOfBirth,
                ContactNumber = form.ContactNumber,
                ResidenceAddress = form.ResidenceAddress,
                Married = form.Married,
                Child = form.Child,
                CurrentOrganization = form.CurrentOrganization,
                ExpectedCTC = form.ExpectedCTC,
                OfferedCTC = form.OfferedCTC,
                JoiningDate = form.JoiningDate,
                NoticePeriod = form.NoticePeriod,
                PreviouslyInterviewed = form.PreviouslyInterviewed,
                Declaration = form.Declaration,
                DigitalSignature = form.DigitalSignature,
                CommunicationSkills = form.CommunicationSkills ?? 5,
                Confidence = form.Confidence ?? 5,
                HRComments = form.HRComments,
                HRSignature = form.HRSignature,
                EvaluationStatus = form.EvaluationStatus,
                TechnicalComments = form.TechnicalComments,
                TechnicalReviewerSignature = form.TechnicalReviewerSignature,
                OtherComments = form.OtherComments,
                WorkExperiences = new List<WorkExperienceEntry>
                {
                    new WorkExperienceEntry { RecordNumber = 1 },
                    new WorkExperienceEntry { RecordNumber = 2 },
                    new WorkExperienceEntry { RecordNumber = 3 }
                },
                EducationRecords = new List<EducationEntry>
                {
                    new EducationEntry { RecordNumber = 1 },
                    new EducationEntry { RecordNumber = 2 },
                    new EducationEntry { RecordNumber = 3 }
                }
            };

            // Overlay existing child records into the fixed slots
            foreach (var we in form.WorkExperiences.OrderBy(w => w.RecordNumber))
            {
                int idx = we.RecordNumber - 1;
                if (idx >= 0 && idx < 3)
                {
                    vm.WorkExperiences[idx] = new WorkExperienceEntry
                    {
                        RecordNumber = we.RecordNumber,
                        CompanyName = we.CompanyName,
                        Designation = we.Designation,
                        StartDate = we.StartDate,
                        EndDate = we.EndDate,
                        DurationOfWork = we.DurationOfWork,
                        LastCTCPerAnnum = we.LastCTCPerAnnum
                    };
                }
            }

            foreach (var edu in form.EducationRecords.OrderBy(e => e.RecordNumber))
            {
                int idx = edu.RecordNumber - 1;
                if (idx >= 0 && idx < 3)
                {
                    vm.EducationRecords[idx] = new EducationEntry
                    {
                        RecordNumber = edu.RecordNumber,
                        DegreeCourse = edu.DegreeCourse,
                        YearOfPassing = edu.YearOfPassing,
                        DivisionPercentage = edu.DivisionPercentage
                    };
                }
            }

            return vm;
        }
    }
}
