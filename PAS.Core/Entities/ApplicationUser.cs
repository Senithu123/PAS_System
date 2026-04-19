using Microsoft.AspNetCore.Identity;

namespace PAS.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}