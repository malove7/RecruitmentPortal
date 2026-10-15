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
        [Route("Auth/Login")]
        [Route("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [Route("Auth/Login")]
        [Route("login")]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            var isAjax = Request.Headers["Accept"].ToString().Contains("application/json") ||
                         Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var token = await _authService.LoginAsync(email, password);

            if (token == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Invalid email or password. Please try again." });
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(12)
            };

            Response.Cookies.Append("jwtToken", token, cookieOptions);

            var redirectTarget = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("Dashboard", "Home") ?? "/Home/Dashboard";

            if (isAjax)
            {
                return Json(new { success = true, redirectUrl = redirectTarget });
            }

            return LocalRedirect(redirectTarget);
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View();
            }

            var result = await _authService.ForgotPasswordAsync(email);

            // We always show a generic success message to prevent email enumeration attacks
            ViewBag.Message = "If your email is registered, you will receive a password reset link shortly.";
            return View();
        }
    }
}
