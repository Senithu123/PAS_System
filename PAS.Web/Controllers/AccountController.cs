using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Infrastructure.Data;
using PAS.Web.ViewModels;
using System.Security.Claims;

namespace PAS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RegisterStudent()
        {
            return View(new RegisterStudentViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStudent(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await EnsureRolesAsync();

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Student");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Dashboard", "Proposal");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RegisterSupervisor()
        {
            return View(new RegisterSupervisorViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterSupervisor(RegisterSupervisorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await EnsureRolesAsync();

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "PendingSupervisor");

            var request = new SupervisorApprovalRequest
            {
                UserId = user.Id,
                FullName = model.FullName,
                Email = model.Email,
                Department = model.Department,
                RequestedExpertise = model.RequestedExpertise,
                Notes = model.Notes,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.SupervisorApprovalRequests.Add(request);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction(nameof(PendingApproval));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName ?? user.Email!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SystemAdmin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "Supervisor"))
            {
                var hasProfile = await _context.SupervisorProfiles.AnyAsync(s => s.SupervisorId == user.Id);

                if (!hasProfile)
                {
                    return RedirectToAction("SetExpertise", "Supervisor");
                }

                return RedirectToAction("Dashboard", "Supervisor");
            }

            if (await _userManager.IsInRoleAsync(user, "PendingSupervisor"))
            {
                return RedirectToAction(nameof(PendingApproval));
            }

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                return RedirectToAction("Dashboard", "Proposal");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PendingApproval()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var request = await _context.SupervisorApprovalRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new PendingSupervisorStatusViewModel
            {
                FullName = request.FullName,
                Email = request.Email,
                Department = request.Department,
                RequestedExpertise = request.RequestedExpertise,
                Notes = request.Notes,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                ReviewedAt = request.ReviewedAt
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}