using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Pr1_ASP.Authorization;

public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement, int>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerOrAdminRequirement requirement, int resourceOwnerId)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
        {
            if (userId == resourceOwnerId) context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}