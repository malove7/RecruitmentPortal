using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class FeedbacksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbacksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Feedbacks
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Feedbacks
                .Include(f => f.Interview)
                    .ThenInclude(i => i.Candidate);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Feedbacks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Interview)
                    .ThenInclude(i => i.Candidate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // GET: Feedbacks/Create
        public IActionResult Create()
        {
            PopulateInterviewsDropDownList();
            return View();
        }

        // POST: Feedbacks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Rating,Comments,InterviewId")] Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                feedback.CreatedDate = DateTime.Now;
                _context.Add(feedback);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateInterviewsDropDownList(feedback.InterviewId);
            return View(feedback);
        }

        // GET: Feedbacks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }
            PopulateInterviewsDropDownList(feedback.InterviewId);
            return View(feedback);
        }

        // POST: Feedbacks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Rating,Comments,InterviewId")] Feedback feedback)
        {
            if (id != feedback.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFeedback = await _context.Feedbacks.FindAsync(id);
                    if (existingFeedback == null)
                    {
                        return NotFound();
                    }

                    existingFeedback.Rating = feedback.Rating;
                    existingFeedback.Comments = feedback.Comments;
                    existingFeedback.InterviewId = feedback.InterviewId;
                    
                    // CreatedDate is preserved

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeedbackExists(feedback.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateInterviewsDropDownList(feedback.InterviewId);
            return View(feedback);
        }

        // GET: Feedbacks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Interview)
                    .ThenInclude(i => i.Candidate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // POST: Feedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.Id == id);
        }

        private void PopulateInterviewsDropDownList(object selectedInterview = null)
        {
            var interviewsQuery = _context.Interviews
                .Include(i => i.Candidate)
                .Select(i => new
                {
                    Id = i.Id,
                    DisplayText = i.Round.ToString() + " - " + (i.Candidate != null ? i.Candidate.FirstName + " " + i.Candidate.LastName : "Unknown Candidate")
                })
                .OrderBy(i => i.DisplayText);

            ViewData["InterviewId"] = new SelectList(interviewsQuery.AsNoTracking(), "Id", "DisplayText", selectedInterview);
        }
    }
}
