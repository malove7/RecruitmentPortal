using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.Enums;

namespace RecruitmentPortal.Controllers
{
    // Secure to Admin only, or those with specific custom permissions if desired.
    // For now, enforcing that only users in the "Admin" role can manage roles.
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError(string.Empty, "Role name cannot be empty.");
                return View();
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError(string.Empty, "A role with this name already exists.");
                return View();
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole(roleName));

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Prevent editing the core Admin role name to avoid breaking the system logic
            if (role.Name == Roles.Admin.ToString())
            {
                TempData["ErrorMessage"] = "Cannot edit the core Admin role name.";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string newRoleName)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            if (role.Name == Roles.Admin.ToString())
            {
                TempData["ErrorMessage"] = "Cannot edit the core Admin role name.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(newRoleName))
            {
                ModelState.AddModelError(string.Empty, "Role name cannot be empty.");
                return View(role); // Pass back the existing role to show the old name
            }

            role.Name = newRoleName;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Prevent deleting essential roles to avoid locking out the admin
            if (role.Name == Roles.Admin.ToString() || role.Name == Roles.Interviewer.ToString())
            {
                TempData["ErrorMessage"] = $"Cannot delete the core {role.Name} role.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Failed to delete role: {errors}";
            }
            else
            {
                TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
