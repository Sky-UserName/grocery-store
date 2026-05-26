using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class IndexModel(SqlDataStore store) : PageModel
{
    public DashboardStats Stats { get; private set; } = new();
    public string ShareUrl { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Stats = await store.GetDashboardStatsAsync();
        ShareUrl = $"{Request.Scheme}://{Request.Host}/mobile/login.html";
    }
}
