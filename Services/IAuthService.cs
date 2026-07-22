namespace RecruitmentPortal.Services
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string email, string password);
        Task<bool> ForgotPasswordAsync(string email);
    }
}
