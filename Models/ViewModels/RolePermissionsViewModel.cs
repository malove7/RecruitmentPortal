namespace RecruitmentPortal.Models.ViewModels
{
    public class RolePermissionsViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<PermissionSelectionViewModel> Permissions { get; set; } = new();
    }

    public class PermissionSelectionViewModel
    {
        public string PermissionName { get; set; } = string.Empty; // e.g., "ViewCandidates"
        public string DisplayName { get; set; } = string.Empty;    // e.g., "View Candidates"
        public string GroupName { get; set; } = string.Empty;      // e.g., "Candidates"
        public bool IsSelected { get; set; }                       // Checked if role has this claim
    }
}
