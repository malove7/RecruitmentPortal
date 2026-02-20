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
    public class InterviewersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Interviewers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Interviewers.ToListAsync());
        }

        // GET: Interviewers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewer == null)
            {
                return NotFound();
            }

            return View(interviewer);
        }

        // GET: Interviewers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Interviewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Department")] Interviewer interviewer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(interviewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(interviewer);
        }

        // GET: Interviewers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewers.FindAsync(id);
            if (interviewer == null)
            {
                return NotFound();
            }
            return View(interviewer);
        }

        // POST: Interviewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Department")] Interviewer interviewer)
        {
            if (id != interviewer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingInterviewer = await _context.Interviewers.FindAsync(id);
                    if (existingInterviewer == null)
                    {
                        return NotFound();
                    }

                    existingInterviewer.Name = interviewer.Name;
                    existingInterviewer.Email = interviewer.Email;
                    existingInterviewer.Department = interviewer.Department;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewerExists(interviewer.Id))
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
            return View(interviewer);
        }

        // GET: Interviewers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewer == null)
            {
                return NotFound();
            }

            return View(interviewer);
        }

        // POST: Interviewers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var interviewer = await _context.Interviewers.FindAsync(id);
            if (interviewer != null)
            {
                _context.Interviewers.Remove(interviewer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InterviewerExists(int id)
        {
            return _context.Interviewers.Any(e => e.Id == id);
        }
    }
}
