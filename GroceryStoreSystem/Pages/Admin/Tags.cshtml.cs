using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GroceryStoreSystem.Pages.Admin;

public sealed class TagsModel(SqlDataStore store) : PageModel
{
    public IReadOnlyList<Tag> Tags { get; private set; } = [];

    [BindProperty]
    public TagInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tags = await store.GetTagsAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            Tags = await store.GetTagsAsync();
            return Page();
        }

        await store.SaveTagAsync(new Tag { Name = Input.Name.Trim(), Color = Input.Color });
        TempData["Success"] = "标签已保存";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        await store.DeleteTagAsync(id);
        TempData["Success"] = "标签已删除";
        return RedirectToPage();
    }

    public sealed class TagInput
    {
        [Required]
        public string Name { get; set; } = "";

        public string? Color { get; set; } = "#2B6EF5";
    }
}
