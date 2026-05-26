namespace GroceryStoreSystem.Models;

public sealed class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
}
