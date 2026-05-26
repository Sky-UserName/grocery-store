namespace GroceryStoreSystem.Models;

public sealed class AdminUser
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int Status { get; set; } = 1;
    public DateTime? LastLoginAt { get; set; }
}
