using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Infrastructure.Data;
using PAS.Web.Models;
using PAS.Web.ViewModels;
using System.Diagnostics;

namespace PAS.Web.Controllers
{
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
            var recentStatuses = await _context.ProjectProposals
                .OrderByDescending(p => p.Id)
                .Take(5)
                .Select(p => $"Proposal #{p.Id} - {p.Status}")
                .ToListAsync();

            var model = new HomeDashboardViewModel
            {
                TotalProposals = await _context.ProjectProposals.CountAsync(),
                TotalResearchAreas = await _context.ResearchAreas.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                RecentStatuses = recentStatuses
            };

            return View(model);
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
}