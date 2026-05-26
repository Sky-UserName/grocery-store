using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GroceryStoreSystem.Pages.Admin;

[AllowAnonymous]
public sealed class LoginModel(SqlDataStore store, PasswordHasher hasher) : PageModel
{
    [BindProperty]
    public string Username { get; set; } = "admin";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Index");
        }

        await Task.CompletedTask;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var admin = await store.GetAdminByUsernameAsync(Username.Trim());
        if (admin is null || admin.Status != 1 || !hasher.Verify(Password, admin.PasswordHash, admin.PasswordSalt))
        {
            ErrorMessage = "账号或密码错误";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        await store.TouchAdminLoginAsync(admin.Id);
        return RedirectToPage("/Admin/Index");
    }
}
