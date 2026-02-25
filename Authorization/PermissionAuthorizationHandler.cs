using Microsoft.AspNetCore.Authorization;

namespace RecruitmentPortal.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null)
            {
                return Task.CompletedTask;
            }

            var hasClaim = context.User.HasClaim(c => c.Type == "Permission" && c.Value == requirement.Permission);
            
            if (hasClaim)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
