
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HotelManagement.Security;

namespace HotelManagement.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            // Tao cac vai tro neu chua ton tai
            string[] roles = { "Admin", "NhanVien", "KhachHang" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        throw new Exception(
                            "Khong the tao vai tro: " + role + ". " +
                            string.Join("; ",
                                roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            var staffRole = await roleManager.FindByNameAsync("NhanVien");
            if (staffRole != null)
            {
                var roleClaims = await roleManager.GetClaimsAsync(staffRole);
                if (!roleClaims.Any(c => c.Type == "hotel:permissions-initialized"))
                {
                    foreach (var module in new[] { "dashboard", "bookings", "rooms", "customers", "finance", "reports" })
                        await roleManager.AddClaimAsync(staffRole, new Claim(ModulePermissionCatalog.ClaimType, module));
                    await roleManager.AddClaimAsync(staffRole, new Claim("hotel:permissions-initialized", "true"));
                }
            }

            // Lay thong tin Admin tu User Secrets hoac cau hinh
            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Thieu SeedAdmin:Email hoac SeedAdmin:Password. " +
                    "Hay cau hinh User Secrets truoc khi chay.");
            }

            // Tim Admin theo email
            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(
                    admin, password);

                if (!createResult.Succeeded)
                {
                    throw new Exception(
                        "Khong the tao tai khoan Admin: " +
                        string.Join("; ",
                            createResult.Errors.Select(e => e.Description)));
                }
            }

            var checkAdmin = await userManager.FindByEmailAsync(email);

            if (checkAdmin == null)
            {
                throw new Exception("Khong tim thay tai khoan Admin trong database.");
            }

            if (!await userManager.HasPasswordAsync(checkAdmin))
            {
                throw new Exception("Tai khoan Admin chua co mat khau.");
            }

            // Dam bao tai khoan duoc gan vai tro Admin
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    admin, "Admin");

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        "Khong the gan vai tro Admin: " +
                        string.Join("; ",
                            roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
