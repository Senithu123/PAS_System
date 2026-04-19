using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Core.Enums;
using PAS.Core.Interfaces;
using PAS.Infrastructure.Data;
using PAS.Web.ViewModels;

namespace PAS.Web.Controllers
{
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IProposalService _proposalService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IProposalService proposalService)
        {
            _userManager = userManager;
            _context = context;
            _proposalService = proposalService;
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

        private async Task<SystemSetting> GetOrCreateSystemSettingAsync()
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync();

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    IsProposalSubmissionOpen = true,
                    IsTopicPublishingOpen = true,
                    IsMatchingOpen = true,
                    AllowFileUploads = true,
                    MaxPreferencesPerStudent = 3,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SystemSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            return setting;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalStudents = 0;
            var totalSupervisors = 0;

            foreach (var user in _userManager.Users)
            {
                if (await _userManager.IsInRoleAsync(user, "Student"))
                    totalStudents++;

                if (await _userManager.IsInRoleAsync(user, "Supervisor"))
                    totalSupervisors++;
            }

            var recentItems = await _context.ProjectProposals
                .OrderByDescending(p => p.Id)
                .Take(6)
                .Select(p => $"{p.Title} - {p.Status}")
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalSupervisors = totalSupervisors,
                TotalProposals = await _context.ProjectProposals.CountAsync(),
                PendingCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Pending),
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.UnderReview),
                MatchedCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Matched),
                RecentItems = recentItems
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var setting = await GetOrCreateSystemSettingAsync();

            var model = new AdminSettingsViewModel
            {
                IsProposalSubmissionOpen = setting.IsProposalSubmissionOpen,
                IsTopicPublishingOpen = setting.IsTopicPublishingOpen,
                IsMatchingOpen = setting.IsMatchingOpen,
                AllowFileUploads = setting.AllowFileUploads,
                MaxPreferencesPerStudent = setting.MaxPreferencesPerStudent
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(AdminSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var setting = await GetOrCreateSystemSettingAsync();

            setting.IsProposalSubmissionOpen = model.IsProposalSubmissionOpen;
            setting.IsTopicPublishingOpen = model.IsTopicPublishingOpen;
            setting.IsMatchingOpen = model.IsMatchingOpen;
            setting.AllowFileUploads = model.AllowFileUploads;
            setting.MaxPreferencesPerStudent = model.MaxPreferencesPerStudent;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "System settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        public async Task<IActionResult> ManageStudents(string? searchTerm)
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var students = new List<AdminStudentViewModel>();

            foreach (var user in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, "Student"))
                    continue;

                var fullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? "");
                var email = user.Email ?? "";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var matches =
                        fullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

                    if (!matches)
                        continue;
                }

                students.Add(new AdminStudentViewModel
                {
                    UserId = user.Id,
                    FullName = fullName,
                    Email = email,
                    TotalProposals = await _context.ProjectProposals.CountAsync(p => p.StudentId == user.Id),
                    MatchedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == user.Id && p.Status == ProposalStatus.Matched)
                });
            }

            var model = new AdminStudentPageViewModel
            {
                TotalStudents = students.Count,
                SearchTerm = searchTerm,
                Students = students.OrderBy(s => s.FullName).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> ManageSupervisors(string? searchTerm)
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var supervisors = new List<AdminSupervisorViewModel>();

            foreach (var user in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, "Supervisor"))
                    continue;

                var fullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? "");
                var email = user.Email ?? "";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var matches =
                        fullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

                    if (!matches)
                        continue;
                }

                var profile = await _context.SupervisorProfiles
                    .Include(s => s.ResearchArea)
                    .FirstOrDefaultAsync(s => s.SupervisorId == user.Id);

                supervisors.Add(new AdminSupervisorViewModel
                {
                    UserId = user.Id,
                    FullName = fullName,
                    Email = email,
                    ExpertiseArea = profile?.ResearchArea?.Name ?? "Not Set",
                    AssignedStudentsCount = await _context.ProjectProposals.CountAsync(p =>
                        p.MatchedSupervisorId == user.Id &&
                        p.Status == ProposalStatus.Matched &&
                        p.IsIdentityRevealed),
                    TopicsSubmittedCount = await _context.SupervisorTopics.CountAsync(t => t.SupervisorId == user.Id)
                });
            }

            var model = new AdminSupervisorPageViewModel
            {
                TotalSupervisors = supervisors.Count,
                SearchTerm = searchTerm,
                Supervisors = supervisors.OrderBy(s => s.FullName).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Results()
        {
            var results = await _context.ProjectProposals
                .Include(p => p.Student)
                .Include(p => p.ResearchArea)
                .Include(p => p.MatchedSupervisor)
                .Where(p => p.Status == ProposalStatus.Matched && p.IsIdentityRevealed)
                .OrderByDescending(p => p.Id)
                .Select(p => new AdminResultViewModel
                {
                    ProposalId = p.Id,
                    StudentName = p.Student != null
                        ? (!string.IsNullOrWhiteSpace(p.Student.FullName)
                            ? p.Student.FullName
                            : p.Student.Email ?? "")
                        : "",
                    StudentEmail = p.Student != null ? p.Student.Email ?? "" : "",
                    ProposalTitle = p.Title,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    SupervisorName = p.MatchedSupervisor != null
                        ? (!string.IsNullOrWhiteSpace(p.MatchedSupervisor.FullName)
                            ? p.MatchedSupervisor.FullName
                            : p.MatchedSupervisor.Email ?? "")
                        : "",
                    SupervisorEmail = p.MatchedSupervisor != null ? p.MatchedSupervisor.Email ?? "" : "",
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Roles = roles.ToList()
                });
            }

            return View(userList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStudentRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, "Student"))
            {
                await _userManager.AddToRoleAsync(user, "Student");
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Proposals(string searchTerm, string statusFilter, int page = 1)
        {
            int pageSize = 5;

            var baseQuery = _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.MatchedSupervisor)
                .AsQueryable();

            var summary = new AdminProposalSummaryViewModel
            {
                TotalProposals = await _context.ProjectProposals.CountAsync(),
                PendingCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Pending),
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.UnderReview),
                MatchedCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Matched),
                RejectedCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Rejected),
                WithdrawnCount = await _context.ProjectProposals.CountAsync(p => p.Status == ProposalStatus.Withdrawn)
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(p =>
                    p.Title.Contains(searchTerm) ||
                    (p.ResearchArea != null && p.ResearchArea.Name.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                Enum.TryParse<ProposalStatus>(statusFilter, out var parsedStatus))
            {
                baseQuery = baseQuery.Where(p => p.Status == parsedStatus);
            }

            var totalItems = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var proposals = await baseQuery
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProposalListViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    Status = p.Status.ToString(),
                    IsIdentityRevealed = p.IsIdentityRevealed,
                    StudentEmail = p.IsIdentityRevealed && p.Student != null
                        ? p.Student.Email ?? ""
                        : "Hidden",
                    SupervisorEmail = p.IsIdentityRevealed && p.MatchedSupervisor != null
                        ? p.MatchedSupervisor.Email
                        : "Not Assigned"
                })
                .ToListAsync();

            var model = new PaginatedAdminProposalPageViewModel
            {
                Summary = summary,
                Proposals = proposals,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var proposal = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.MatchedSupervisor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound();
            }

            var model = new ProposalDetailsViewModel
            {
                Id = proposal.Id,
                Title = proposal.Title,
                Abstract = proposal.Abstract,
                TechnicalStack = proposal.TechnicalStack,
                ResearchArea = proposal.ResearchArea != null ? proposal.ResearchArea.Name : "",
                Status = proposal.Status.ToString(),
                IsIdentityRevealed = proposal.IsIdentityRevealed,
                StudentEmail = proposal.IsIdentityRevealed && proposal.Student != null
                    ? proposal.Student.Email ?? ""
                    : "Hidden",
                SupervisorEmail = proposal.IsIdentityRevealed && proposal.MatchedSupervisor != null
                    ? proposal.MatchedSupervisor.Email ?? ""
                    : "Not Assigned"
            };

            return View(model);
        }

        public async Task<IActionResult> Report()
        {
            var reportData = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.MatchedSupervisor)
                .OrderByDescending(p => p.Id)
                .Select(p => new ProposalDetailsViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechnicalStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    Status = p.Status.ToString(),
                    IsIdentityRevealed = p.IsIdentityRevealed,
                    StudentEmail = p.IsIdentityRevealed && p.Student != null
                        ? p.Student.Email ?? ""
                        : "Hidden",
                    SupervisorEmail = p.IsIdentityRevealed && p.MatchedSupervisor != null
                        ? p.MatchedSupervisor.Email ?? ""
                        : "Not Assigned"
                })
                .ToListAsync();

            return View(reportData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProposalStatus(int id, string status)
        {
            var proposal = await _context.ProjectProposals
                .Include(p => p.Student)
                .Include(p => p.MatchedSupervisor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<ProposalStatus>(status, out var parsedStatus))
            {
                proposal.Status = parsedStatus;
                proposal.IsIdentityRevealed = parsedStatus == ProposalStatus.Matched;
                proposal.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(proposal.StudentId))
                {
                    await AddNotificationAsync(
                        proposal.StudentId,
                        "Proposal Status Updated",
                        $"Your proposal '{proposal.Title}' status changed to {proposal.Status}.",
                        "Proposal");
                }

                if (!string.IsNullOrWhiteSpace(proposal.MatchedSupervisorId))
                {
                    await AddNotificationAsync(
                        proposal.MatchedSupervisorId,
                        "Proposal Status Updated",
                        $"Proposal '{proposal.Title}' status changed to {proposal.Status}.",
                        "Proposal");
                }
            }

            return RedirectToAction(nameof(Proposals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMatch(int id)
        {
            var setting = await GetOrCreateSystemSettingAsync();

            if (!setting.IsMatchingOpen)
            {
                TempData["ErrorMessage"] = "Matching is currently closed in system settings.";
                return RedirectToAction(nameof(Proposals));
            }

            var proposal = await _context.ProjectProposals
                .Include(p => p.Student)
                .Include(p => p.MatchedSupervisor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound();
            }

            var success = await _proposalService.ConfirmMatchAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = "No supervisor has expressed interest in this proposal yet.";
                return RedirectToAction(nameof(Proposals));
            }

            if (!string.IsNullOrWhiteSpace(proposal.StudentId))
            {
                await AddNotificationAsync(
                    proposal.StudentId,
                    "Match Confirmed",
                    $"Your proposal '{proposal.Title}' has been matched and confirmed by admin.",
                    "Match");
            }

            if (!string.IsNullOrWhiteSpace(proposal.MatchedSupervisorId))
            {
                await AddNotificationAsync(
                    proposal.MatchedSupervisorId,
                    "Final Match Confirmed",
                    $"You have been officially matched with proposal '{proposal.Title}'.",
                    "Match");
            }

            TempData["SuccessMessage"] = "Match confirmed and identities revealed successfully.";
            return RedirectToAction(nameof(Proposals));
        }
    }
}