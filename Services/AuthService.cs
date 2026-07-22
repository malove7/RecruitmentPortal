using Microsoft.AspNetCore.Identity;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!isValidPassword)
            {
                return null;
            }

            return await _jwtTokenGenerator.GenerateTokenAsync(user);
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            // In a real application, you would generate a password reset token here and email it.
            // var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // await _emailService.SendEmailAsync(email, "Reset Password", $"Your reset code is {token}");

            return true;
        }
    }
}
