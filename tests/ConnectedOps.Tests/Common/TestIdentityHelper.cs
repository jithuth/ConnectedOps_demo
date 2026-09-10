using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Tests.Common;

public static class TestIdentityHelper
{
    public static UserManager<ApplicationUser> CreateUserManager(ConnectedOpsDbContext context)
    {
        var userStore = new UserStore<ApplicationUser, IdentityRole<Guid>, ConnectedOpsDbContext, Guid>(context);
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var options = Options.Create(new IdentityOptions
        {
            Password =
            {
                RequireDigit = true,
                RequiredLength = 8,
                RequireNonAlphanumeric = false,
                RequireUppercase = false,
                RequireLowercase = false
            }
        });

        return new UserManager<ApplicationUser>(
            userStore,
            options,
            passwordHasher,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory()));
    }
}
