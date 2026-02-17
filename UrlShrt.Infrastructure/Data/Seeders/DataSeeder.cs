using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrlShrt.Domain.Entities;

namespace UrlShrt.Infrastructure.Data.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "User", "ApiUser" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            const string adminEmail = "admin@urlshortener.com";
            if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

            var admin = new ApplicationUser
            {
                FirstName = "Super",
                LastName = "Admin",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                Plan = "Enterprise",
                UrlLimit = int.MaxValue
            };

            var result = await userManager.CreateAsync(admin, "Admin@123456!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
