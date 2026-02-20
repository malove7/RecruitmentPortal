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
    public class CandidatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Candidates
        public async Task<IActionResult> Index(string searchString, int? jobPositionId)
        {
            var candidates = _context.Candidates.Include(c => c.JobPosition).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                candidates = candidates.Where(s => s.FirstName.Contains(searchString) || s.LastName.Contains(searchString) || s.Email.Contains(searchString));
            }

            if (jobPositionId.HasValue)
            {
                candidates = candidates.Where(x => x.JobPositionId == jobPositionId);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["JobPositions"] = new SelectList(_context.JobPositions, "Id", "Title");
            ViewData["CurrentJobPosition"] = jobPositionId;

            return View(await candidates.ToListAsync());
        }

        // GET: Candidates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates
                .Include(c => c.JobPosition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidate == null)
            {
                return NotFound();
            }

            return View(candidate);
        }

        // GET: Candidates/Create
        public IActionResult Create()
        {
            ViewData["JobPositionId"] = new SelectList(_context.JobPositions, "Id", "Description");
            return View();
        }

        // POST: Candidates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Email,Phone,JobPositionId")] Candidate candidate, IFormFile? resumeFile)
        {
            if (ModelState.IsValid)
            {
                if (resumeFile != null && resumeFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + resumeFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await resumeFile.CopyToAsync(stream);
                    }

                    candidate.ResumePath = "/uploads/" + uniqueFileName;
                }
                
                // Set default values
                candidate.AppliedDate = DateTime.Now;
                candidate.Status = CandidateStatus.Applied;

                _context.Add(candidate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["JobPositionId"] = new SelectList(_context.JobPositions, "Id", "Description", candidate.JobPositionId);
            return View(candidate);
        }

        // GET: Candidates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            ViewData["JobPositionId"] = new SelectList(_context.JobPositions, "Id", "Description", candidate.JobPositionId);
            return View(candidate);
        }

        // POST: Candidates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Phone,JobPositionId")] Candidate candidate)
        {
            if (id != candidate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCandidate = await _context.Candidates.FindAsync(id);
                    if (existingCandidate == null)
                    {
                        return NotFound();
                    }

                    existingCandidate.FirstName = candidate.FirstName;
                    existingCandidate.LastName = candidate.LastName;
                    existingCandidate.Email = candidate.Email;
                    existingCandidate.Phone = candidate.Phone;
                    existingCandidate.JobPositionId = candidate.JobPositionId;
                    
                    // ResumePath, AppliedDate, Status are preserved

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidateExists(candidate.Id))
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
            ViewData["JobPositionId"] = new SelectList(_context.JobPositions, "Id", "Description", candidate.JobPositionId);
            return View(candidate);
        }

        // GET: Candidates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidate = await _context.Candidates
                .Include(c => c.JobPosition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidate == null)
            {
                return NotFound();
            }

            return View(candidate);
        }

        // POST: Candidates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate != null)
            {
                _context.Candidates.Remove(candidate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CandidateExists(int id)
        {
            return _context.Candidates.Any(e => e.Id == id);
        }
    }
}
