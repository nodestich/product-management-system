using Microsoft.AspNetCore.Identity;

namespace ProductManagement.Infrastructure.Persistence.Identity;

public static class IdentitySeeder
{
    public const string DemoEmail = "admin@example.com";
    public const string DemoPassword = "Admin123!";

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, DemoPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed the demo user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}