using Microsoft.AspNetCore.Identity;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.Enums;
using System.Security.Claims;

namespace RecruitmentPortal.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed Roles
            var roles = Enum.GetValues(typeof(Roles)).Cast<Roles>().Select(r => r.ToString());
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            // 2. Assign Permissions to Admin Role (Admin gets all permissions)
            var adminRole = await roleManager.FindByNameAsync(Roles.Admin.ToString());
            if (adminRole != null)
            {
                var existingClaims = await roleManager.GetClaimsAsync(adminRole);
                var allPermissions = Enum.GetValues(typeof(Permissions)).Cast<Permissions>();

                foreach (var permission in allPermissions)
                {
                    var permissionClaim = new Claim("Permission", permission.ToString());
                    if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission.ToString()))
                    {
                        await roleManager.AddClaimAsync(adminRole, permissionClaim);
                    }
                }
            }

            // Assign permissions to HR role
            var hrRole = await roleManager.FindByNameAsync(Roles.HR.ToString());
            if (hrRole != null)
            {
                var hrExisting = await roleManager.GetClaimsAsync(hrRole);
                var hrPermissions = new[]
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewCandidates, Permissions.CreateCandidates, Permissions.EditCandidates,
                    Permissions.ViewJobPositions,
                    Permissions.ViewInterviews, Permissions.CreateInterviews, Permissions.EditInterviews,
                    Permissions.ViewFeedbacks, Permissions.CreateFeedbacks, Permissions.EditFeedbacks,
                    Permissions.ViewEvaluationForms, Permissions.EditEvaluationForms, Permissions.DeleteEvaluationForms
                };
                foreach (var p in hrPermissions)
                {
                    var claim = new Claim("Permission", p.ToString());
                    if (!hrExisting.Any(c => c.Type == "Permission" && c.Value == p.ToString()))
                        await roleManager.AddClaimAsync(hrRole, claim);
                }
            }

            // Assign permissions to Interviewer role
            var interviewerRole = await roleManager.FindByNameAsync(Roles.Interviewer.ToString());
            if (interviewerRole != null)
            {
                var intExisting = await roleManager.GetClaimsAsync(interviewerRole);
                var interviewerPermissions = new[]
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewCandidates,
                    Permissions.ViewJobPositions,
                    Permissions.ViewInterviews,
                    Permissions.ViewFeedbacks,
                    Permissions.ViewEvaluationForms
                };
                foreach (var p in interviewerPermissions)
                {
                    var claim = new Claim("Permission", p.ToString());
                    if (!intExisting.Any(c => c.Type == "Permission" && c.Value == p.ToString()))
                        await roleManager.AddClaimAsync(interviewerRole, claim);
                }
            }

            // 3. Seed Default Admin User
            string adminEmail = "admin@recruitmentportal.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());
                }
            }
        }
    }
}
