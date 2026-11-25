using System.Security.Claims;
using Hospital.BLL.Helpers;
using Hospital.BLL.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Infrastructure;

/// <summary>
/// Projects the signed-in user's roles into explicit permission claims once per request.
/// Because the projection happens server-side on every authenticated request, revoking a
/// role takes effect immediately without re-issuing the cookie.
/// </summary>
public sealed class PermissionClaimsTransformation(IServiceScopeFactory scopeFactory) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var identity = (ClaimsIdentity)principal.Identity!;

        // Guarantee real Full Name is present even on pre-existing cookies
        var givenName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(givenName) && !string.IsNullOrWhiteSpace(userId))
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<Hospital.DAL.DataBase.HospitalDbContext>();
            if (db is not null)
            {
                var u = await db.Users.Where(x => x.Id == userId)
                    .Select(x => new { x.FirstName, x.LastName })
                    .FirstOrDefaultAsync();
                if (u is not null)
                {
                    var fullName = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.GivenName, fullName));
                    }
                }
            }
        }

        if (principal.HasClaim(claim => claim.Type == AppClaimTypes.Permission))
        {
            return principal;
        }

        var roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToList();

        foreach (var role in roles)
        {
            foreach (var permission in RolePermissions.ForName(role))
            {
                if (!principal.HasClaim(AppClaimTypes.Permission, permission))
                {
                    identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
                }
            }

            if (RolePermissions.IsClinician(ParseRole(role)))
            {
                if (!principal.HasClaim(AppClaimTypes.Clinical, "true"))
                {
                    identity.AddClaim(new Claim(AppClaimTypes.Clinical, "true"));
                }
            }
        }

        return principal;
    }

    private static Role ParseRole(string roleName) =>
        Enum.TryParse<Role>(roleName, ignoreCase: true, out var role) ? role : Role.Patient;
}
