using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Infrastructure.Data;
using PAS.Web.ViewModels;

namespace PAS.Web.Controllers
{
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class SupervisorRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SupervisorRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private async Task EnsureRolesAsync()
        {
            var roles = new[] { "Student", "Supervisor", "Admin", "SystemAdmin", "PendingSupervisor" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task AddNotificationAsync(string userId, string title, string message, string type = "Info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.SupervisorApprovalRequests
                .OrderBy(r => r.Status == "Pending" ? 0 : 1)
                .ThenByDescending(r => r.RequestedAt)
                .Select(r => new SupervisorApprovalRequestListItemViewModel
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    FullName = r.FullName,
                    Email = r.Email,
                    Department = r.Department,
                    RequestedExpertise = r.RequestedExpertise,
                    Notes = r.Notes,
                    Status = r.Status,
                    RequestedAt = r.RequestedAt,
                    ReviewedAt = r.ReviewedAt
                })
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await EnsureRolesAsync();

            var request = await _context.SupervisorApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "PendingSupervisor"))
            {
                await _userManager.RemoveFromRoleAsync(user, "PendingSupervisor");
            }

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Student");
            }

            if (!await _userManager.IsInRoleAsync(user, "Supervisor"))
            {
                await _userManager.AddToRoleAsync(user, "Supervisor");
            }

            request.Status = "Approved";
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                user.Id,
                "Supervisor Request Approved",
                "Your supervisor application has been approved. Log in as a supervisor and set your expertise.",
                "Approval");

            TempData["SuccessMessage"] = "Supervisor request approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.SupervisorApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Rejected";
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                request.UserId,
                "Supervisor Request Rejected",
                "Your supervisor application has been rejected. Please contact the administrator for details.",
                "Approval");

            TempData["SuccessMessage"] = "Supervisor request rejected successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}