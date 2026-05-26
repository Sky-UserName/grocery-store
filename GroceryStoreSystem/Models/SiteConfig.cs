namespace GroceryStoreSystem.Models;

public sealed class SiteConfig
{
    public long Id { get; set; } = 1;
    public string SiteName { get; set; } = "梦梦杂货铺";
    public string SiteSubtitle { get; set; } = "每日更新 稳定靠谱";
    public string SiteDescription { get; set; } = "认准梦梦不迷路";
    public string AccessPasswordHash { get; set; } = "";
    public string AccessPasswordSalt { get; set; } = "";
    public bool AccessPasswordEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
