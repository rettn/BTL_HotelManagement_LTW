using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace HotelManagement.Security;

public sealed class ModulePermissionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RoleManager<IdentityRole> roleManager)
    {
        if (context.User.Identity?.IsAuthenticated == true && !context.User.IsInRole("Admin"))
        {
            var module = ModulePermissionCatalog.GetModule(
                context.GetRouteData().Values["controller"]?.ToString(),
                context.GetRouteData().Values["action"]?.ToString());

            if (module is not null && context.User.IsInRole("NhanVien"))
            {
                var role = await roleManager.FindByNameAsync("NhanVien");
                var claims = role is null ? [] : await roleManager.GetClaimsAsync(role);
                var allowed = claims.Any(c => c.Type == ModulePermissionCatalog.ClaimType && c.Value == module);
                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Tài khoản chưa được cấp quyền truy cập chức năng này.");
                    return;
                }
            }
        }

        await next(context);
    }
}
