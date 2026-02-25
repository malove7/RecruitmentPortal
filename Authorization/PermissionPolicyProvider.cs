using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace RecruitmentPortal.Authorization
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if there's a predefined policy first
            var policy = await base.GetPolicyAsync(policyName);
            if (policy != null)
            {
                return policy;
            }

            // Dynamically create policy for Permission requirements
            if (policyName.StartsWith("Permissions.", StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName.Substring(12); // "Permissions.".Length
                var policyBuilder = new AuthorizationPolicyBuilder();
                policyBuilder.AddRequirements(new PermissionRequirement(permission));
                return policyBuilder.Build();
            }

            return null;
        }
    }
}
