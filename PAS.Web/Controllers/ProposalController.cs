using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Core.Enums;
using PAS.Infrastructure.Data;
using PAS.Web.ViewModels;
using System.Security.Claims;

namespace PAS.Web.Controllers
{
    [Authorize]
    public class ProposalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
        private const long MaxFileSize = 10 * 1024 * 1024;

        public ProposalController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        private async Task<SystemSetting> GetSystemSettingAsync()
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

        private string GetUploadsFolder()
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads", "proposals");
            Directory.CreateDirectory(uploadsFolder);

            return uploadsFolder;
        }

        private async Task<(bool Success, string? SavedFileName, string? OriginalFileName, string? ErrorMessage)> SaveProposalFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (true, null, null, null);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, null, null, "Only PDF, DOC, and DOCX files are allowed.");
            }

            if (file.Length > MaxFileSize)
            {
                return (false, null, null, "The file size must be 10 MB or less.");
            }

            var safeOriginalName = Path.GetFileName(file.FileName);
            var savedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(GetUploadsFolder(), savedFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, savedFileName, safeOriginalName, null);
        }

        private void DeleteProposalFile(string? savedFileName)
        {
            if (string.IsNullOrWhiteSpace(savedFileName))
            {
                return;
            }

            var fullPath = Path.Combine(GetUploadsFolder(), savedFileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var recentItems = await _context.ProjectProposals
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .Select(p => $"{p.Title} - {p.Status}")
                .ToListAsync();

            var model = new StudentDashboardViewModel
            {
                TotalProposals = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId),
                PendingCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Pending),
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.UnderReview),
                MatchedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Matched),
                RejectedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Rejected),
                WithdrawnCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Withdrawn),
                RecentItems = recentItems
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new StudentProfilePageViewModel
            {
                FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? ""),
                Email = user.Email ?? "",
                Role = "Student",
                TotalProposals = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId),
                MatchedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Matched),
                PendingCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && (p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview))
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AvailableProjects(string? searchTerm)
        {
            var query = _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Where(p => p.Status == ProposalStatus.Matched || p.Status == ProposalStatus.UnderReview || p.Status == ProposalStatus.Pending)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchTerm) ||
                    (p.ResearchArea != null && p.ResearchArea.Name.Contains(searchTerm)) ||
                    p.TechnicalStack.Contains(searchTerm));
            }

            var projects = await query
                .OrderByDescending(p => p.Id)
                .Select(p => new AvailableProjectViewModel
                {
                    ProposalId = p.Id,
                    Title = p.Title,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    TechnicalStack = p.TechnicalStack,
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> MyPreferences()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var availableProjects = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Where(p => p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview || p.Status == ProposalStatus.Matched)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.AvailableProjects = new SelectList(
                availableProjects.Select(p => new
                {
                    Id = p.Id,
                    Name = $"{p.Title} ({(p.ResearchArea != null ? p.ResearchArea.Name : "No Area")})"
                }),
                "Id",
                "Name"
            );

            var preferences = await _context.StudentPreferences
                .Include(p => p.ProjectProposal)
                .ThenInclude(pp => pp.ResearchArea)
                .Where(p => p.StudentId == studentId)
                .OrderBy(p => p.PreferenceRank)
                .Select(p => new StudentPreferenceItemViewModel
                {
                    Id = p.Id,
                    ProjectProposalId = p.ProjectProposalId,
                    ProjectTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : "",
                    ResearchArea = p.ProjectProposal != null && p.ProjectProposal.ResearchArea != null
                        ? p.ProjectProposal.ResearchArea.Name
                        : "",
                    PreferenceRank = p.PreferenceRank
                })
                .ToListAsync();

            var model = new StudentPreferencesPageViewModel
            {
                Input = new StudentPreferenceInputViewModel(),
                Preferences = preferences
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyPreferences(StudentPreferencesPageViewModel model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var setting = await GetSystemSettingAsync();

            var availableProjects = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Where(p => p.Status == ProposalStatus.Pending || p.Status == ProposalStatus.UnderReview || p.Status == ProposalStatus.Matched)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.AvailableProjects = new SelectList(
                availableProjects.Select(p => new
                {
                    Id = p.Id,
                    Name = $"{p.Title} ({(p.ResearchArea != null ? p.ResearchArea.Name : "No Area")})"
                }),
                "Id",
                "Name",
                model.Input.ProjectProposalId
            );

            if (!ModelState.IsValid)
            {
                model.Preferences = await LoadStudentPreferencesAsync(studentId);
                return View(model);
            }

            var duplicateProject = await _context.StudentPreferences.AnyAsync(p =>
                p.StudentId == studentId &&
                p.ProjectProposalId == model.Input.ProjectProposalId);

            if (duplicateProject)
            {
                ModelState.AddModelError("", "You have already added this project to your preferences.");
            }

            var duplicateRank = await _context.StudentPreferences.AnyAsync(p =>
                p.StudentId == studentId &&
                p.PreferenceRank == model.Input.PreferenceRank);

            if (duplicateRank)
            {
                ModelState.AddModelError("", "This preference rank is already used. Please choose a different rank.");
            }

            var currentPreferenceCount = await _context.StudentPreferences.CountAsync(p => p.StudentId == studentId);

            if (currentPreferenceCount >= setting.MaxPreferencesPerStudent)
            {
                ModelState.AddModelError("", $"You can only save up to {setting.MaxPreferencesPerStudent} preferences.");
            }

            if (!ModelState.IsValid)
            {
                model.Preferences = await LoadStudentPreferencesAsync(studentId);
                return View(model);
            }

            var projectTitle = await _context.ProjectProposals
                .Where(p => p.Id == model.Input.ProjectProposalId)
                .Select(p => p.Title)
                .FirstOrDefaultAsync() ?? "project";

            var preference = new StudentPreference
            {
                StudentId = studentId,
                ProjectProposalId = model.Input.ProjectProposalId,
                PreferenceRank = model.Input.PreferenceRank
            };

            _context.StudentPreferences.Add(preference);
            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                studentId,
                "Preference Saved",
                $"You saved '{projectTitle}' as preference rank {model.Input.PreferenceRank}.",
                "Preference");

            TempData["SuccessMessage"] = "Preference saved successfully.";
            return RedirectToAction(nameof(MyPreferences));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePreference(int id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var preference = await _context.StudentPreferences
                .Include(p => p.ProjectProposal)
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);

            if (preference == null)
            {
                return NotFound();
            }

            var projectTitle = preference.ProjectProposal?.Title ?? "project";

            _context.StudentPreferences.Remove(preference);
            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                studentId,
                "Preference Removed",
                $"You removed '{projectTitle}' from your saved preferences.",
                "Preference");

            TempData["SuccessMessage"] = "Preference removed successfully.";
            return RedirectToAction(nameof(MyPreferences));
        }

        [HttpGet]
        public async Task<IActionResult> MatchResult()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Login", "Account");
            }

            var proposal = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.MatchedSupervisor)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (proposal == null)
            {
                var emptyModel = new StudentMatchResultViewModel
                {
                    HasProposal = false,
                    IsMatched = false,
                    Message = "You have not submitted a proposal yet."
                };

                return View(emptyModel);
            }

            var model = new StudentMatchResultViewModel
            {
                HasProposal = true,
                IsMatched = proposal.Status == ProposalStatus.Matched && proposal.IsIdentityRevealed,
                ProposalTitle = proposal.Title,
                Status = proposal.Status.ToString(),
                ResearchArea = proposal.ResearchArea != null ? proposal.ResearchArea.Name : "",
                SupervisorName = proposal.IsIdentityRevealed && proposal.MatchedSupervisor != null
                    ? (!string.IsNullOrWhiteSpace(proposal.MatchedSupervisor.FullName)
                        ? proposal.MatchedSupervisor.FullName
                        : proposal.MatchedSupervisor.Email ?? "")
                    : "Hidden until confirmed",
                SupervisorEmail = proposal.IsIdentityRevealed && proposal.MatchedSupervisor != null
                    ? proposal.MatchedSupervisor.Email ?? ""
                    : "Hidden until confirmed",
                Message = proposal.Status == ProposalStatus.Matched && proposal.IsIdentityRevealed
                    ? "Your final match has been confirmed."
                    : "Matching is still in progress. Please check again later."
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name");

            return View(new CreateProposalViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProposalViewModel model)
        {
            await PopulateResearchAreasAsync(model.ResearchAreaId);

            var setting = await GetSystemSettingAsync();

            if (!setting.IsProposalSubmissionOpen)
            {
                ModelState.AddModelError("", "Proposal submission is currently closed by the administrator.");
                return View(model);
            }

            if (!setting.AllowFileUploads && model.ProposalFile != null)
            {
                ModelState.AddModelError("ProposalFile", "File uploads are currently disabled by the administrator.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                ModelState.AddModelError("", "You must be logged in to submit a proposal.");
                return View(model);
            }

            var hasActiveProposal = await _context.ProjectProposals.AnyAsync(p =>
                p.StudentId == studentId &&
                p.Status != ProposalStatus.Withdrawn &&
                p.Status != ProposalStatus.Rejected);

            if (hasActiveProposal)
            {
                ModelState.AddModelError("", "You already have an active proposal in the system.");
                return View(model);
            }

            var fileResult = await SaveProposalFileAsync(model.ProposalFile);
            if (!fileResult.Success)
            {
                ModelState.AddModelError("ProposalFile", fileResult.ErrorMessage!);
                return View(model);
            }

            var proposal = new ProjectProposal
            {
                Title = model.Title,
                Abstract = model.Abstract,
                TechnicalStack = model.TechnicalStack,
                ResearchAreaId = model.ResearchAreaId,
                StudentId = studentId,
                Status = ProposalStatus.Pending,
                IsIdentityRevealed = false,
                FilePath = fileResult.SavedFileName,
                FileName = fileResult.OriginalFileName
            };

            _context.ProjectProposals.Add(proposal);
            await _context.SaveChangesAsync();

            var message = fileResult.OriginalFileName == null
                ? $"Your proposal '{proposal.Title}' was submitted successfully."
                : $"Your proposal '{proposal.Title}' was submitted successfully with file '{fileResult.OriginalFileName}'.";

            await AddNotificationAsync(studentId, "Proposal Submitted", message, "Proposal");

            TempData["SuccessMessage"] = "Project proposal submitted successfully.";
            return RedirectToAction(nameof(MyProposals));
        }

        [HttpGet]
        public async Task<IActionResult> MyProposals(int page = 1)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction(nameof(Create));
            }

            int pageSize = 5;

            var query = _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.MatchedSupervisor)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Id);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var summary = new StudentProposalSummaryViewModel
            {
                TotalProposals = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId),
                PendingCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Pending),
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.UnderReview),
                MatchedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Matched),
                RejectedCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Rejected),
                WithdrawnCount = await _context.ProjectProposals.CountAsync(p => p.StudentId == studentId && p.Status == ProposalStatus.Withdrawn)
            };

            var proposals = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new StudentProposalViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    Status = p.Status.ToString(),
                    IsIdentityRevealed = p.IsIdentityRevealed,
                    SupervisorEmail = p.IsIdentityRevealed && p.MatchedSupervisor != null
                        ? p.MatchedSupervisor.Email ?? "Not Available"
                        : "Hidden until matched",
                    FileName = p.FileName,
                    HasUploadedFile = !string.IsNullOrWhiteSpace(p.FilePath)
                })
                .ToListAsync();

            var model = new PaginatedStudentProposalPageViewModel
            {
                Proposals = proposals,
                Summary = summary,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction(nameof(MyProposals));
            }

            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);

            if (proposal == null)
            {
                return NotFound();
            }

            if (proposal.Status == ProposalStatus.Matched)
            {
                return Forbid();
            }

            await PopulateResearchAreasAsync(proposal.ResearchAreaId);

            var model = new CreateProposalViewModel
            {
                Title = proposal.Title,
                Abstract = proposal.Abstract,
                TechnicalStack = proposal.TechnicalStack,
                ResearchAreaId = proposal.ResearchAreaId,
                CurrentFileName = proposal.FileName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProposalViewModel model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction(nameof(MyProposals));
            }

            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);

            await PopulateResearchAreasAsync(model.ResearchAreaId);

            if (proposal == null)
            {
                return NotFound();
            }

            if (proposal.Status == ProposalStatus.Matched)
            {
                return Forbid();
            }

            model.CurrentFileName = proposal.FileName;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var setting = await GetSystemSettingAsync();

            if (!setting.AllowFileUploads && model.ProposalFile != null)
            {
                ModelState.AddModelError("ProposalFile", "File uploads are currently disabled by the administrator.");
                return View(model);
            }

            var oldSavedFile = proposal.FilePath;

            if (model.ProposalFile != null)
            {
                var fileResult = await SaveProposalFileAsync(model.ProposalFile);
                if (!fileResult.Success)
                {
                    ModelState.AddModelError("ProposalFile", fileResult.ErrorMessage!);
                    return View(model);
                }

                proposal.FilePath = fileResult.SavedFileName;
                proposal.FileName = fileResult.OriginalFileName;
            }

            proposal.Title = model.Title;
            proposal.Abstract = model.Abstract;
            proposal.TechnicalStack = model.TechnicalStack;
            proposal.ResearchAreaId = model.ResearchAreaId;
            proposal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (model.ProposalFile != null)
            {
                DeleteProposalFile(oldSavedFile);
            }

            var updateMessage = model.ProposalFile == null
                ? $"Your proposal '{proposal.Title}' was updated successfully."
                : $"Your proposal '{proposal.Title}' was updated and file '{proposal.FileName}' was uploaded.";

            await AddNotificationAsync(studentId, "Proposal Updated", updateMessage, "Proposal");

            TempData["SuccessMessage"] = "Proposal updated successfully.";
            return RedirectToAction(nameof(MyProposals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction(nameof(MyProposals));
            }

            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == studentId);

            if (proposal == null)
            {
                return NotFound();
            }

            if (proposal.Status == ProposalStatus.Matched)
            {
                return Forbid();
            }

            proposal.Status = ProposalStatus.Withdrawn;
            proposal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                studentId,
                "Proposal Withdrawn",
                $"Your proposal '{proposal.Title}' was withdrawn.",
                "Proposal");

            TempData["SuccessMessage"] = "Proposal withdrawn successfully.";
            return RedirectToAction(nameof(MyProposals));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadProposalFile(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null || string.IsNullOrWhiteSpace(proposal.FilePath) || string.IsNullOrWhiteSpace(proposal.FileName))
            {
                return NotFound();
            }

            var isOwner = proposal.StudentId == currentUserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SystemAdmin");

            if (!isOwner && !isAdmin)
            {
                return Forbid();
            }

            var fullPath = Path.Combine(GetUploadsFolder(), proposal.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(proposal.FileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(fullPath, contentType, proposal.FileName);
        }

        private async Task PopulateResearchAreasAsync(int? selectedId = null)
        {
            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", selectedId);
        }

        private async Task<List<StudentPreferenceItemViewModel>> LoadStudentPreferencesAsync(string studentId)
        {
            return await _context.StudentPreferences
                .Include(p => p.ProjectProposal)
                .ThenInclude(pp => pp.ResearchArea)
                .Where(p => p.StudentId == studentId)
                .OrderBy(p => p.PreferenceRank)
                .Select(p => new StudentPreferenceItemViewModel
                {
                    Id = p.Id,
                    ProjectProposalId = p.ProjectProposalId,
                    ProjectTitle = p.ProjectProposal != null ? p.ProjectProposal.Title : "",
                    ResearchArea = p.ProjectProposal != null && p.ProjectProposal.ResearchArea != null
                        ? p.ProjectProposal.ResearchArea.Name
                        : "",
                    PreferenceRank = p.PreferenceRank
                })
                .ToListAsync();
        }
    }
}