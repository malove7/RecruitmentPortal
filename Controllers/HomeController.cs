using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.ViewModels;

using Microsoft.AspNetCore.Authorization;

namespace RecruitmentPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel
        {
            TotalCandidates = await _context.Candidates.CountAsync(),
            TotalOpenPositions = await _context.JobPositions.CountAsync(j => j.IsActive),
            InterviewsScheduledToday = await _context.Interviews.CountAsync(i => i.ScheduledTime.Date == DateTime.Today),
            PendingFeedbacks = await _context.Interviews.CountAsync(i => i.ScheduledTime < DateTime.Now && i.Feedback == null),
            RecentCandidates = await _context.Candidates
                .OrderByDescending(c => c.AppliedDate)
                .Take(5)
                .Include(c => c.JobPosition)
                .ToListAsync(),
            UpcomingInterviews = await _context.Interviews
                .Where(i => i.ScheduledTime >= DateTime.Now)
                .OrderBy(i => i.ScheduledTime)
                .Take(5)
                .Include(i => i.Candidate)
                .Include(i => i.Interviewer)
                .ToListAsync()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
