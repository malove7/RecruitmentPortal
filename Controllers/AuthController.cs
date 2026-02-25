using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var token = await _authService.LoginAsync(email, password);

            if (token == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Ensure this is fine for local dev (requires HTTPS)
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(12) 
            };

            Response.Cookies.Append("jwtToken", token, cookieOptions);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
