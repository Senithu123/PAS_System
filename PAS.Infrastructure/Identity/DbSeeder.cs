using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAS.Core.Entities;
using PAS.Infrastructure.Data;

namespace PAS.Infrastructure.Identity
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndResearchAreasAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            string[] roles = { "Student", "Supervisor", "Admin", "SystemAdmin" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            if (!await context.ResearchAreas.AnyAsync())
            {
                var researchAreas = new List<ResearchArea>
                {
                    new ResearchArea { Name = "Artificial Intelligence", Description = "AI, ML, deep learning, intelligent systems" },
                    new ResearchArea { Name = "Web Development", Description = "Frontend, backend, full-stack systems" },
                    new ResearchArea { Name = "Cybersecurity", Description = "Security, networks, ethical hacking, secure systems" },
                    new ResearchArea { Name = "Cloud Computing", Description = "Cloud platforms, distributed systems, virtualization" },
                    new ResearchArea { Name = "Data Science", Description = "Data analysis, big data, predictive analytics" },
                    new ResearchArea { Name = "Software Engineering", Description = "Software design, testing, architecture, DevOps" }
                };

                await context.ResearchAreas.AddRangeAsync(researchAreas);
                await context.SaveChangesAsync();
            }

            string adminEmail = "admin@pas.com";
            string adminPassword = "Admin123";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
            }
        }
    }
}