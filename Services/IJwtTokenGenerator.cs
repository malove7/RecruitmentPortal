using RecruitmentPortal.Models;

namespace RecruitmentPortal.Services
{
    public interface IJwtTokenGenerator
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
    }
}
