using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.Enums;
using RecruitmentPortal.Models.ViewModels;
using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;

namespace RecruitmentPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PermissionsController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public PermissionsController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            var model = new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                Permissions = new List<PermissionSelectionViewModel>()
            };

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

            // Populate all available permissions from Enum
            foreach (var permission in Enum.GetValues(typeof(Permissions)).Cast<Permissions>())
            {
                var permissionName = permission.ToString();
                model.Permissions.Add(new PermissionSelectionViewModel
                {
                    PermissionName = permissionName,
                    DisplayName = GetEnumDescription(permission),
                    // Determine the "Group" by the naming convention (e.g., "ViewCandidates" -> "Candidates")
                    GroupName = GetGroupName(permissionName),
                    IsSelected = permissionClaims.Contains(permissionName)
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(RolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
            {
                return NotFound();
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingPermissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

            // Find all selected permissions in the incoming model
            var selectedPermissions = model.Permissions.Where(p => p.IsSelected).Select(p => p.PermissionName).ToList();

            // Remove claims that are no longer selected, unless it's the Admin role (Admin should always have everything)
            if (role.Name != RecruitmentPortal.Models.Enums.Roles.Admin.ToString())
            {
                var claimsToRemove = existingPermissionClaims.Where(c => !selectedPermissions.Contains(c.Value)).ToList();
                foreach (var claim in claimsToRemove)
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }

                // Add newly selected claims
                var claimsToAdd = selectedPermissions.Where(p => !existingPermissionClaims.Any(c => c.Value == p)).ToList();
                foreach (var claimValue in claimsToAdd)
                {
                    await _roleManager.AddClaimAsync(role, new Claim("Permission", claimValue));
                }
            }
            else
            {
                 TempData["SuccessMessage"] = "Admin role permissions cannot be customized. They automatically carry all permissions.";
                 return RedirectToAction("Index", "Roles");
            }

            TempData["SuccessMessage"] = $"Permissions updated successfully for role '{role.Name}'.";
            return RedirectToAction("Index", "Roles");
        }

        private string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }

        private string GetGroupName(string permissionName)
        {
            if (permissionName.EndsWith("Candidates")) return "Candidates";
            if (permissionName.EndsWith("JobPositions")) return "Job Positions";
            if (permissionName.EndsWith("Interviews")) return "Interviews";
            if (permissionName.EndsWith("Interviewers")) return "Interviewers";
            if (permissionName.EndsWith("Feedbacks")) return "Feedbacks";
            if (permissionName.EndsWith("Dashboard")) return "General";
            return "Other";
        }
    }
}
