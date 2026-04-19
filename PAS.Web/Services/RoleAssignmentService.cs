using Microsoft.AspNetCore.Identity;
using PAS.Core.Entities;

namespace PAS.Web.Services
{
    public class RoleAssignmentService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleAssignmentService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task AssignStudentRoleAsync(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, "Student"))
            {
                await _userManager.AddToRoleAsync(user, "Student");
            }
        }
    }
}