namespace GroceryStoreSystem.Models;

public static class CardStatus
{
    public const int Draft = 0;
    public const int Published = 1;
    public const int Hidden = 2;

    public static string Label(int status) => status switch
    {
        Published => "已发布",
        Hidden => "隐藏",
        _ => "草稿"
    };
}

public sealed class CardListItem
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public int Status { get; set; }
    public int SortOrder { get; set; }
    public string Tags { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CardDetail
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public string ContentHtml { get; set; } = "";
    public int Status { get; set; } = CardStatus.Draft;
    public int SortOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Tag> Tags { get; set; } = [];
}

public sealed class DashboardStats
{
    public int CategoryCount { get; set; }
    public int CardCount { get; set; }
    public int PublishedCount { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

public sealed class UploadFileRecord
{
    public long Id { get; set; }
    public string OriginalName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public long FileSize { get; set; }
    public string MimeType { get; set; } = "";
    public string UsageType { get; set; } = "";
}
