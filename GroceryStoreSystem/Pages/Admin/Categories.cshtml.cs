using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class CategoriesModel(SqlDataStore store) : PageModel
{
    public IReadOnlyList<Category> Categories { get; private set; } = [];

    [BindProperty]
    public CategoryInput Input { get; set; } = new();

    public async Task OnGetAsync(long? id)
    {
        Categories = await store.GetCategoriesAsync();
        if (id is null)
        {
            Input = new CategoryInput { IsEnabled = true, SortOrder = NextSort() };
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == id.Value);
        if (category is not null)
        {
            Input = new CategoryInput
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                SortOrder = category.SortOrder,
                IsEnabled = category.IsEnabled
            };
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Slug))
        {
            Input.Slug = Slugify(Input.Name);
        }

        if (!ModelState.IsValid)
        {
            Categories = await store.GetCategoriesAsync();
            return Page();
        }

        await store.SaveCategoryAsync(new Category
        {
            Id = Input.Id,
            Name = Input.Name.Trim(),
            Slug = Input.Slug.Trim(),
            SortOrder = Input.SortOrder,
            IsEnabled = Input.IsEnabled
        });

        TempData["Success"] = "分类已保存";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        var count = await store.CountCardsByCategoryAsync(id);
        if (count > 0)
        {
            TempData["Error"] = "该分类下已有卡片，不能直接删除";
            return RedirectToPage();
        }

        await store.DeleteCategoryAsync(id);
        TempData["Success"] = "分类已删除";
        return RedirectToPage();
    }

    private int NextSort()
    {
        return Categories.Count == 0 ? 10 : Categories.Max(c => c.SortOrder) + 10;
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^\w]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"category-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" : slug;
    }

    public sealed class CategoryInput
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string Slug { get; set; } = "";

        public int SortOrder { get; set; } = 10;

        public bool IsEnabled { get; set; } = true;
    }
}
