using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class InterviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Interviews
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Interviews.Include(i => i.Candidate).Include(i => i.Interviewer);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Interviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Interviewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interview == null)
            {
                return NotFound();
            }

            return View(interview);
        }

        // GET: Interviews/Create
        public async Task<IActionResult> Create()
        {
            var interviewers = await _userManager.GetUsersInRoleAsync("Interviewer");
            ViewData["CandidateId"] = new SelectList(_context.Candidates, "Id", "Email");
            ViewData["InterviewerId"] = new SelectList(interviewers, "Id", "Email");
            return View();
        }

        // POST: Interviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ScheduledTime,Round,Notes,CandidateId,InterviewerId")] Interview interview)
        {
            if (ModelState.IsValid)
            {
                _context.Add(interview);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var interviewers = await _userManager.GetUsersInRoleAsync("Interviewer");
            ViewData["CandidateId"] = new SelectList(_context.Candidates, "Id", "Email", interview.CandidateId);
            ViewData["InterviewerId"] = new SelectList(interviewers, "Id", "Email", interview.InterviewerId);
            return View(interview);
        }

        // GET: Interviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null)
            {
                return NotFound();
            }
            var interviewers = await _userManager.GetUsersInRoleAsync("Interviewer");
            ViewData["CandidateId"] = new SelectList(_context.Candidates, "Id", "Email", interview.CandidateId);
            ViewData["InterviewerId"] = new SelectList(interviewers, "Id", "Email", interview.InterviewerId);
            return View(interview);
        }

        // POST: Interviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ScheduledTime,Round,Notes,CandidateId,InterviewerId")] Interview interview)
        {
            if (id != interview.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingInterview = await _context.Interviews.FindAsync(id);
                    if (existingInterview == null)
                    {
                        return NotFound();
                    }

                    existingInterview.ScheduledTime = interview.ScheduledTime;
                    existingInterview.Round = interview.Round;
                    existingInterview.Notes = interview.Notes;
                    existingInterview.CandidateId = interview.CandidateId;
                    existingInterview.InterviewerId = interview.InterviewerId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewExists(interview.Id))
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
            var interviewers = await _userManager.GetUsersInRoleAsync("Interviewer");
            ViewData["CandidateId"] = new SelectList(_context.Candidates, "Id", "Email", interview.CandidateId);
            ViewData["InterviewerId"] = new SelectList(interviewers, "Id", "Email", interview.InterviewerId);
            return View(interview);
        }

        // GET: Interviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interview = await _context.Interviews
                .Include(i => i.Candidate)
                .Include(i => i.Interviewer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interview == null)
            {
                return NotFound();
            }

            return View(interview);
        }

        // POST: Interviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview != null)
            {
                _context.Interviews.Remove(interview);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InterviewExists(int id)
        {
            return _context.Interviews.Any(e => e.Id == id);
        }
    }
}
