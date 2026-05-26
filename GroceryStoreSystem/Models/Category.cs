namespace GroceryStoreSystem.Models;

public sealed class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
