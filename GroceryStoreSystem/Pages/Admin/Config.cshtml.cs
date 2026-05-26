using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class ConfigModel(SqlDataStore store, PasswordHasher hasher) : PageModel
{
    [BindProperty]
    public ConfigInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var config = await store.GetSiteConfigAsync();
        Input = new ConfigInput
        {
            SiteName = config.SiteName,
            SiteSubtitle = config.SiteSubtitle,
            SiteDescription = config.SiteDescription,
            AccessPasswordEnabled = config.AccessPasswordEnabled
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var config = await store.GetSiteConfigAsync();
        config.SiteName = Input.SiteName.Trim();
        config.SiteSubtitle = Input.SiteSubtitle.Trim();
        config.SiteDescription = Input.SiteDescription.Trim();
        config.AccessPasswordEnabled = Input.AccessPasswordEnabled;
        await store.SaveSiteConfigAsync(config, Input.NewAccessPassword, hasher);

        TempData["Success"] = "系统配置已保存";
        return RedirectToPage();
    }

    public sealed class ConfigInput
    {
        [Required]
        public string SiteName { get; set; } = "";

        [Required]
        public string SiteSubtitle { get; set; } = "";

        [Required]
        public string SiteDescription { get; set; } = "";

        public bool AccessPasswordEnabled { get; set; } = true;

        public string? NewAccessPassword { get; set; }
    }
}
