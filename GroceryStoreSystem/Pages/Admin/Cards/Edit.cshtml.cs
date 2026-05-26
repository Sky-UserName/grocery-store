using GroceryStoreSystem.Models;
using GroceryStoreSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GroceryStoreSystem.Pages.Admin.Cards;

public sealed class EditModel(SqlDataStore store) : PageModel
{
    public IReadOnlyList<Category> Categories { get; private set; } = [];

    [BindProperty]
    public CardInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long? id)
    {
        Categories = await store.GetCategoriesAsync();
        if (Categories.Count == 0)
        {
            TempData["Error"] = "请先创建分类";
            return RedirectToPage("/Admin/Categories");
        }

        if (id is null)
        {
            Input = new CardInput
            {
                CategoryId = Categories[0].Id,
                Status = CardStatus.Draft,
                SortOrder = 10,
                ContentHtml = "<p>请输入详情内容。</p>"
            };
            return Page();
        }

        var card = await store.GetCardAsync(id.Value);
        if (card is null)
        {
            return NotFound();
        }

        Input = new CardInput
        {
            Id = card.Id,
            CategoryId = card.CategoryId,
            Title = card.Title,
            Summary = card.Summary,
            CoverImageUrl = card.CoverImageUrl,
            ContentHtml = card.ContentHtml,
            Status = card.Status,
            SortOrder = card.SortOrder,
            TagsText = string.Join(",", card.Tags.Select(t => t.Name))
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await store.GetCategoriesAsync();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var card = new CardDetail
        {
            Id = Input.Id,
            CategoryId = Input.CategoryId,
            Title = Input.Title.Trim(),
            Summary = Input.Summary.Trim(),
            CoverImageUrl = Input.CoverImageUrl,
            ContentHtml = Input.ContentHtml,
            Status = Input.Status,
            SortOrder = Input.SortOrder,
            Tags = (Input.TagsText ?? "")
                .Split(',', '，', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(name => new Tag { Name = name })
                .ToList()
        };

        var id = await store.SaveCardAsync(card);
        TempData["Success"] = "卡片已保存";
        return RedirectToPage("/Admin/Cards/Edit", new { id });
    }

    public sealed class CardInput
    {
        public long Id { get; set; }

        [Required]
        public long CategoryId { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Summary { get; set; } = "";

        public string? CoverImageUrl { get; set; }

        [Required]
        public string ContentHtml { get; set; } = "";

        public int Status { get; set; } = CardStatus.Draft;

        public int SortOrder { get; set; } = 10;

        public string? TagsText { get; set; }
    }
}
