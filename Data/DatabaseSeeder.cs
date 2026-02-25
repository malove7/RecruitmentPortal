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
