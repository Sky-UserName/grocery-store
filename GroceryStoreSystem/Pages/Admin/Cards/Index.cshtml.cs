using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GroceryStoreSystem.Pages.Admin.Cards;

public sealed class IndexModel(SqlDataStore store) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public long? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public IReadOnlyList<CardListItem> Cards { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(long id)
    {
        await store.SetCardStatusAsync(id, CardStatus.Published);
        TempData["Success"] = "卡片已发布";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHideAsync(long id)
    {
        await store.SetCardStatusAsync(id, CardStatus.Hidden);
        TempData["Success"] = "卡片已隐藏";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        await store.DeleteCardAsync(id);
        TempData["Success"] = "卡片已删除";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Categories = await store.GetCategoriesAsync();
        Cards = await store.GetCardsAsync(CategoryId, Status, Keyword);
    }
}
