using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class ChangePasswordModel(SqlDataStore store, PasswordHasher hasher) : PageModel
{
    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.NewPassword != Input.ConfirmPassword)
        {
            TempData["Error"] = "两次输入的新密码不一致";
            return Page();
        }

        var username = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var admin = await store.GetAdminByUsernameAsync(username);
        if (admin is null || !hasher.Verify(Input.CurrentPassword, admin.PasswordHash, admin.PasswordSalt))
        {
            TempData["Error"] = "原密码错误";
            return Page();
        }

        var (hash, salt) = hasher.Create(Input.NewPassword);
        await store.ChangeAdminPasswordAsync(admin.Id, hash, salt);
        TempData["Success"] = "后台登录密码已修改";
        return RedirectToPage();
    }

    public sealed class PasswordInput
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = "";

        [Required]
        public string ConfirmPassword { get; set; } = "";
    }
}
