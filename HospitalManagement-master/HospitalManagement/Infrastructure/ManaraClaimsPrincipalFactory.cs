using System.Security.Claims;
using Hospital.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Infrastructure;

/// <summary>
/// Ensures every issued Identity cookie carries the user's real display full name
/// in <see cref="ClaimTypes.GivenName"/> instead of falling back to the login username.
/// </summary>
public sealed class ManaraClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            foreach (var existing in identity.FindAll(ClaimTypes.GivenName).ToList())
            {
                identity.RemoveClaim(existing);
            }

            identity.AddClaim(new Claim(ClaimTypes.GivenName, fullName));
        }

        return identity;
    }
}
