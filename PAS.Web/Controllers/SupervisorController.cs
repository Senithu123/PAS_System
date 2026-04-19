using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx" };
        private const long MaxFileSize = 10 * 1024 * 1024;

        public SupervisorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
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

        private string GetTopicUploadsFolder()
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads", "topics");
            Directory.CreateDirectory(uploadsFolder);

            return uploadsFolder;
        }

        private string GetProposalUploadsFolder()
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

        private async Task<(bool Success, string? SavedFileName, string? OriginalFileName, string? ErrorMessage)> SaveTopicFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return (true, null, null, null);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, null, null, "Only PDF, DOC, DOCX, PPT, and PPTX files are allowed.");
            }

            if (file.Length > MaxFileSize)
            {
                return (false, null, null, "The file size must be 10 MB or less.");
            }

            var safeOriginalName = Path.GetFileName(file.FileName);
            var savedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(GetTopicUploadsFolder(), savedFileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, savedFileName, safeOriginalName, null);
        }

        private void DeleteTopicFile(string? savedFileName)
        {
            if (string.IsNullOrWhiteSpace(savedFileName))
            {
                return;
            }

            var fullPath = Path.Combine(GetTopicUploadsFolder(), savedFileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _context.SupervisorProfiles
                .Include(s => s.ResearchArea)
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            var expertise = profile?.ResearchArea?.Name ?? "Not Set";

            var available = 0;
            if (profile != null)
            {
                available = await _context.ProjectProposals.CountAsync(p =>
                    p.Status == ProposalStatus.Pending &&
                    p.ResearchAreaId == profile.ResearchAreaId);
            }

            var recentItems = await _context.ProjectProposals
                .Where(p => p.MatchedSupervisorId == supervisorId)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .Select(p => $"{p.Title} - {p.Status}")
                .ToListAsync();

            var model = new SupervisorDashboardViewModel
            {
                ExpertiseArea = expertise,
                AvailableToReview = available,
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p =>
                    p.MatchedSupervisorId == supervisorId && p.Status == ProposalStatus.UnderReview),
                MatchedCount = await _context.ProjectProposals.CountAsync(p =>
                    p.MatchedSupervisorId == supervisorId && p.Status == ProposalStatus.Matched),
                TopicsSubmittedCount = await _context.SupervisorTopics.CountAsync(t => t.SupervisorId == supervisorId),
                RecentItems = recentItems
            };

            return View(model);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(supervisorId);

            if (user == null)
            {
                return NotFound();
            }

            var profile = await _context.SupervisorProfiles
                .Include(s => s.ResearchArea)
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            var model = new SupervisorProfilePageViewModel
            {
                FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Email ?? ""),
                Email = user.Email ?? "",
                Role = "Supervisor",
                ExpertiseArea = profile?.ResearchArea?.Name ?? "Not Set",
                AssignedStudentsCount = await _context.ProjectProposals.CountAsync(p =>
                    p.MatchedSupervisorId == supervisorId &&
                    p.Status == ProposalStatus.Matched &&
                    p.IsIdentityRevealed),
                UnderReviewCount = await _context.ProjectProposals.CountAsync(p =>
                    p.MatchedSupervisorId == supervisorId &&
                    p.Status == ProposalStatus.UnderReview)
            };

            return View(model);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> SubmitTopic()
        {
            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name");
            ViewBag.DifficultyLevels = new SelectList(new List<string> { "Beginner", "Intermediate", "Advanced" });

            return View(new SupervisorTopicViewModel());
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTopic(SupervisorTopicViewModel model, string submitAction)
        {
            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", model.ResearchAreaId);
            ViewBag.DifficultyLevels = new SelectList(
                new List<string> { "Beginner", "Intermediate", "Advanced" },
                model.DifficultyLevel);

            var setting = await GetSystemSettingAsync();

            if (submitAction == "Publish" && !setting.IsTopicPublishingOpen)
            {
                ModelState.AddModelError("", "Topic publishing is currently closed by the administrator.");
            }

            if (!setting.AllowFileUploads && model.AttachmentFile != null)
            {
                ModelState.AddModelError("AttachmentFile", "File uploads are currently disabled by the administrator.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            var fileResult = await SaveTopicFileAsync(model.AttachmentFile);
            if (!fileResult.Success)
            {
                ModelState.AddModelError("AttachmentFile", fileResult.ErrorMessage!);
                return View(model);
            }

            var topic = new SupervisorTopic
            {
                ProjectTitle = model.ProjectTitle,
                ResearchAreaId = model.ResearchAreaId,
                Description = model.Description,
                RequiredSkills = model.RequiredSkills,
                ExpectedOutcomes = model.ExpectedOutcomes,
                Capacity = model.Capacity,
                DifficultyLevel = model.DifficultyLevel,
                SupervisorId = supervisorId,
                Status = submitAction == "Publish" ? "Published" : "Draft",
                AttachmentFilePath = fileResult.SavedFileName,
                AttachmentFileName = fileResult.OriginalFileName
            };

            _context.SupervisorTopics.Add(topic);
            await _context.SaveChangesAsync();

            var message = submitAction == "Publish"
                ? $"Your topic '{topic.ProjectTitle}' was published successfully."
                : $"Your topic '{topic.ProjectTitle}' was saved as draft.";

            if (!string.IsNullOrWhiteSpace(topic.AttachmentFileName))
            {
                message += $" Attached file: '{topic.AttachmentFileName}'.";
            }

            await AddNotificationAsync(
                supervisorId,
                submitAction == "Publish" ? "Topic Published" : "Topic Saved",
                message,
                "Topic");

            TempData["SuccessMessage"] = submitAction == "Publish"
                ? "Project topic published successfully."
                : "Project topic saved as draft.";

            return RedirectToAction(nameof(MyTopics));
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> MyTopics()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var topics = await _context.SupervisorTopics
                .Include(t => t.ResearchArea)
                .Where(t => t.SupervisorId == supervisorId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new SupervisorTopicListViewModel
                {
                    Id = t.Id,
                    ProjectTitle = t.ProjectTitle,
                    ResearchArea = t.ResearchArea != null ? t.ResearchArea.Name : "",
                    DifficultyLevel = t.DifficultyLevel,
                    Capacity = t.Capacity,
                    Status = t.Status,
                    AttachmentFileName = t.AttachmentFileName,
                    HasAttachment = !string.IsNullOrWhiteSpace(t.AttachmentFilePath),
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return View(topics);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> EditTopic(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            var topic = await _context.SupervisorTopics
                .FirstOrDefaultAsync(t => t.Id == id && t.SupervisorId == supervisorId);

            if (topic == null)
            {
                return NotFound();
            }

            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", topic.ResearchAreaId);
            ViewBag.DifficultyLevels = new SelectList(
                new List<string> { "Beginner", "Intermediate", "Advanced" },
                topic.DifficultyLevel);

            var model = new SupervisorTopicViewModel
            {
                ProjectTitle = topic.ProjectTitle,
                ResearchAreaId = topic.ResearchAreaId,
                Description = topic.Description,
                RequiredSkills = topic.RequiredSkills,
                ExpectedOutcomes = topic.ExpectedOutcomes,
                Capacity = topic.Capacity,
                DifficultyLevel = topic.DifficultyLevel,
                CurrentAttachmentFileName = topic.AttachmentFileName
            };

            return View(model);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTopic(int id, SupervisorTopicViewModel model, string submitAction)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            var topic = await _context.SupervisorTopics
                .FirstOrDefaultAsync(t => t.Id == id && t.SupervisorId == supervisorId);

            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", model.ResearchAreaId);
            ViewBag.DifficultyLevels = new SelectList(
                new List<string> { "Beginner", "Intermediate", "Advanced" },
                model.DifficultyLevel);

            if (topic == null)
            {
                return NotFound();
            }

            model.CurrentAttachmentFileName = topic.AttachmentFileName;

            var setting = await GetSystemSettingAsync();

            if (submitAction == "Publish" && !setting.IsTopicPublishingOpen)
            {
                ModelState.AddModelError("", "Topic publishing is currently closed by the administrator.");
            }

            if (!setting.AllowFileUploads && model.AttachmentFile != null)
            {
                ModelState.AddModelError("AttachmentFile", "File uploads are currently disabled by the administrator.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oldSavedFile = topic.AttachmentFilePath;

            if (model.AttachmentFile != null)
            {
                var fileResult = await SaveTopicFileAsync(model.AttachmentFile);
                if (!fileResult.Success)
                {
                    ModelState.AddModelError("AttachmentFile", fileResult.ErrorMessage!);
                    return View(model);
                }

                topic.AttachmentFilePath = fileResult.SavedFileName;
                topic.AttachmentFileName = fileResult.OriginalFileName;
            }

            topic.ProjectTitle = model.ProjectTitle;
            topic.ResearchAreaId = model.ResearchAreaId;
            topic.Description = model.Description;
            topic.RequiredSkills = model.RequiredSkills;
            topic.ExpectedOutcomes = model.ExpectedOutcomes;
            topic.Capacity = model.Capacity;
            topic.DifficultyLevel = model.DifficultyLevel;
            topic.Status = submitAction == "Publish" ? "Published" : "Draft";
            topic.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (model.AttachmentFile != null)
            {
                DeleteTopicFile(oldSavedFile);
            }

            await AddNotificationAsync(
                supervisorId,
                "Topic Updated",
                $"Your topic '{topic.ProjectTitle}' was updated successfully.",
                "Topic");

            TempData["SuccessMessage"] = "Project topic updated successfully.";
            return RedirectToAction(nameof(MyTopics));
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction("Login", "Account");
            }

            var topic = await _context.SupervisorTopics
                .FirstOrDefaultAsync(t => t.Id == id && t.SupervisorId == supervisorId);

            if (topic == null)
            {
                return NotFound();
            }

            var topicTitle = topic.ProjectTitle;
            var savedFile = topic.AttachmentFilePath;

            _context.SupervisorTopics.Remove(topic);
            await _context.SaveChangesAsync();

            DeleteTopicFile(savedFile);

            await AddNotificationAsync(
                supervisorId,
                "Topic Deleted",
                $"Your topic '{topicTitle}' was deleted.",
                "Topic");

            TempData["SuccessMessage"] = "Project topic deleted successfully.";
            return RedirectToAction(nameof(MyTopics));
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> DownloadTopicAttachment(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var topic = await _context.SupervisorTopics
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null || string.IsNullOrWhiteSpace(topic.AttachmentFilePath) || string.IsNullOrWhiteSpace(topic.AttachmentFileName))
            {
                return NotFound();
            }

            var isOwner = topic.SupervisorId == currentUserId;
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SystemAdmin");

            if (!isOwner && !isAdmin)
            {
                return Forbid();
            }

            var fullPath = Path.Combine(GetTopicUploadsFolder(), topic.AttachmentFilePath);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(topic.AttachmentFileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(fullPath, contentType, topic.AttachmentFileName);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
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

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SystemAdmin");
            var isSupervisor = User.IsInRole("Supervisor");
            var isMatchedSupervisor = proposal.MatchedSupervisorId == currentUserId;

            if (!isAdmin && !isSupervisor && !isMatchedSupervisor)
            {
                return Forbid();
            }

            var fullPath = Path.Combine(GetProposalUploadsFolder(), proposal.FilePath);

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

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> AssignedStudents()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var students = await _context.ProjectProposals
                .Include(p => p.Student)
                .Include(p => p.ResearchArea)
                .Where(p =>
                    p.MatchedSupervisorId == supervisorId &&
                    p.Status == ProposalStatus.Matched &&
                    p.IsIdentityRevealed)
                .OrderByDescending(p => p.Id)
                .Select(p => new AssignedStudentViewModel
                {
                    ProposalId = p.Id,
                    StudentName = p.Student != null
                        ? (!string.IsNullOrWhiteSpace(p.Student.FullName) ? p.Student.FullName : p.Student.Email ?? "")
                        : "",
                    StudentEmail = p.Student != null ? p.Student.Email ?? "" : "",
                    ProposalTitle = p.Title,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            return View(students);
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> SetExpertise()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name");

            var existingProfile = await _context.SupervisorProfiles
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            if (existingProfile != null)
            {
                var model = new SupervisorProfileViewModel
                {
                    ResearchAreaId = existingProfile.ResearchAreaId
                };

                ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", existingProfile.ResearchAreaId);
                return View(model);
            }

            return View(new SupervisorProfileViewModel());
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetExpertise(SupervisorProfileViewModel model)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var researchAreas = await _context.ResearchAreas
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.ResearchAreas = new SelectList(researchAreas, "Id", "Name", model.ResearchAreaId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingProfile = await _context.SupervisorProfiles
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            if (existingProfile == null)
            {
                var profile = new SupervisorProfile
                {
                    SupervisorId = supervisorId ?? string.Empty,
                    ResearchAreaId = model.ResearchAreaId
                };

                _context.SupervisorProfiles.Add(profile);
            }
            else
            {
                existingProfile.ResearchAreaId = model.ResearchAreaId;
                existingProfile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Supervisor expertise saved successfully.";
            return RedirectToAction(nameof(SetExpertise));
        }

        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> Browse()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var profile = await _context.SupervisorProfiles
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Please set your preferred research area first.";
                return RedirectToAction(nameof(SetExpertise));
            }

            var proposals = await _context.ProjectProposals
                .Include(p => p.ResearchArea)
                .Where(p =>
                    p.ResearchAreaId == profile.ResearchAreaId &&
                    (
                        (p.Status == ProposalStatus.Pending && p.MatchedSupervisorId == null) ||
                        (p.Status == ProposalStatus.UnderReview && p.MatchedSupervisorId == supervisorId)
                    ))
                .OrderByDescending(p => p.Id)
                .Select(p => new SupervisorProposalViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechnicalStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea != null ? p.ResearchArea.Name : "",
                    Status = p.Status.ToString(),
                    HasUploadedFile = !string.IsNullOrWhiteSpace(p.FilePath),
                    FileName = p.FileName,
                    CanReview = p.Status == ProposalStatus.Pending && p.MatchedSupervisorId == null,
                    CanMatch = p.Status == ProposalStatus.Pending || (p.Status == ProposalStatus.UnderReview && p.MatchedSupervisorId == supervisorId)
                })
                .ToListAsync();

            return View(proposals);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        public async Task<IActionResult> ExpressInterest(int id)
        {
            var setting = await GetSystemSettingAsync();

            if (!setting.IsMatchingOpen)
            {
                TempData["ErrorMessage"] = "Matching is currently closed by the administrator.";
                return RedirectToAction(nameof(Browse));
            }

            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction(nameof(Browse));
            }

            var profile = await _context.SupervisorProfiles
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Please set your expertise first.";
                return RedirectToAction(nameof(SetExpertise));
            }

            var proposal = await _context.ProjectProposals.FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound();
            }

            if (proposal.ResearchAreaId != profile.ResearchAreaId)
            {
                return Forbid();
            }

            if (proposal.Status == ProposalStatus.Matched || proposal.Status == ProposalStatus.Withdrawn)
            {
                return RedirectToAction(nameof(Browse));
            }

            if (proposal.Status == ProposalStatus.UnderReview &&
                !string.IsNullOrWhiteSpace(proposal.MatchedSupervisorId) &&
                proposal.MatchedSupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Another supervisor is already reviewing this proposal.";
                return RedirectToAction(nameof(Browse));
            }

            proposal.Status = ProposalStatus.UnderReview;
            proposal.MatchedSupervisorId = supervisorId;
            proposal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                supervisorId,
                "Proposal Under Review",
                $"You marked proposal '{proposal.Title}' as under review.",
                "Review");

            if (!string.IsNullOrWhiteSpace(proposal.StudentId))
            {
                await AddNotificationAsync(
                    proposal.StudentId,
                    "Proposal Under Review",
                    $"Your proposal '{proposal.Title}' is now under review by a supervisor.",
                    "Proposal");
            }

            TempData["SuccessMessage"] = "Proposal marked as under review.";
            return RedirectToAction(nameof(Browse));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor,Admin,SystemAdmin")]
        public async Task<IActionResult> MatchProposal(int id)
        {
            var setting = await GetSystemSettingAsync();

            if (!setting.IsMatchingOpen)
            {
                TempData["ErrorMessage"] = "Matching is currently closed by the administrator.";
                return RedirectToAction(nameof(Browse));
            }

            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(supervisorId))
            {
                return RedirectToAction(nameof(Browse));
            }

            var profile = await _context.SupervisorProfiles
                .FirstOrDefaultAsync(s => s.SupervisorId == supervisorId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Please set your expertise first.";
                return RedirectToAction(nameof(SetExpertise));
            }

            var proposal = await _context.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proposal == null)
            {
                return NotFound();
            }

            if (proposal.ResearchAreaId != profile.ResearchAreaId)
            {
                return Forbid();
            }

            if (proposal.Status == ProposalStatus.Matched || proposal.Status == ProposalStatus.Withdrawn)
            {
                TempData["ErrorMessage"] = "This proposal cannot be matched.";
                return RedirectToAction(nameof(Browse));
            }

            if (proposal.Status == ProposalStatus.UnderReview &&
                !string.IsNullOrWhiteSpace(proposal.MatchedSupervisorId) &&
                proposal.MatchedSupervisorId != supervisorId)
            {
                TempData["ErrorMessage"] = "Another supervisor is already reviewing this proposal.";
                return RedirectToAction(nameof(Browse));
            }

            proposal.Status = ProposalStatus.Matched;
            proposal.MatchedSupervisorId = supervisorId;
            proposal.IsIdentityRevealed = true;
            proposal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddNotificationAsync(
                supervisorId,
                "Proposal Matched",
                $"You successfully matched with proposal '{proposal.Title}'.",
                "Match");

            if (!string.IsNullOrWhiteSpace(proposal.StudentId))
            {
                await AddNotificationAsync(
                    proposal.StudentId,
                    "Proposal Matched",
                    $"Your proposal '{proposal.Title}' has been matched by a supervisor.",
                    "Match");
            }

            TempData["SuccessMessage"] = "Proposal matched successfully.";
            return RedirectToAction(nameof(AssignedStudents));
        }
    }
}