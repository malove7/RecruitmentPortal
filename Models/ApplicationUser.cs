using Microsoft.AspNetCore.Identity;

namespace RecruitmentPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // Navigation properties if needed (e.g., Interviews managed by this user)
    }
}
