using GroceryStoreSystem.Models;
using Microsoft.Data.SqlClient;

namespace GroceryStoreSystem.Services;

public sealed class DatabaseInitializer(SqlDataStore store, PasswordHasher hasher)
{
    public async Task InitializeAsync()
    {
        await EnsureDatabaseAsync();
        await EnsureTablesAsync();
        await SeedAsync();
    }

    private async Task EnsureDatabaseAsync()
    {
        var builder = new SqlConnectionStringBuilder(store.ConnectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE {QuoteIdentifier(databaseName)}';
                EXEC(@sql);
            END
            """;
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTablesAsync()
    {
        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'AdminUsers', N'U') IS NULL
            CREATE TABLE AdminUsers (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Username nvarchar(50) NOT NULL UNIQUE,
                PasswordHash nvarchar(255) NOT NULL,
                PasswordSalt nvarchar(255) NOT NULL,
                Status int NOT NULL CONSTRAINT DF_AdminUsers_Status DEFAULT 1,
                LastLoginAt datetime2 NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_AdminUsers_CreatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedAt datetime2 NOT NULL CONSTRAINT DF_AdminUsers_UpdatedAt DEFAULT SYSUTCDATETIME()
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'SiteConfig', N'U') IS NULL
            CREATE TABLE SiteConfig (
                Id bigint NOT NULL PRIMARY KEY,
                SiteName nvarchar(100) NOT NULL,
                SiteSubtitle nvarchar(200) NOT NULL,
                SiteDescription nvarchar(500) NOT NULL,
                AccessPasswordHash nvarchar(255) NOT NULL,
                AccessPasswordSalt nvarchar(255) NOT NULL,
                AccessPasswordEnabled bit NOT NULL,
                UpdatedAt datetime2 NOT NULL CONSTRAINT DF_SiteConfig_UpdatedAt DEFAULT SYSUTCDATETIME()
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'Categories', N'U') IS NULL
            CREATE TABLE Categories (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Name nvarchar(100) NOT NULL,
                Slug nvarchar(100) NOT NULL UNIQUE,
                Icon nvarchar(255) NULL,
                SortOrder int NOT NULL,
                IsEnabled bit NOT NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedAt datetime2 NOT NULL CONSTRAINT DF_Categories_UpdatedAt DEFAULT SYSUTCDATETIME()
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'Cards', N'U') IS NULL
            CREATE TABLE Cards (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                CategoryId bigint NOT NULL,
                Title nvarchar(200) NOT NULL,
                Summary nvarchar(500) NOT NULL,
                CoverImageUrl nvarchar(500) NULL,
                ContentHtml nvarchar(max) NOT NULL,
                Status int NOT NULL,
                SortOrder int NOT NULL,
                PublishedAt datetime2 NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Cards_CreatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedAt datetime2 NOT NULL CONSTRAINT DF_Cards_UpdatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT FK_Cards_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'Tags', N'U') IS NULL
            CREATE TABLE Tags (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Name nvarchar(50) NOT NULL UNIQUE,
                Color nvarchar(30) NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_Tags_CreatedAt DEFAULT SYSUTCDATETIME()
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'CardTags', N'U') IS NULL
            CREATE TABLE CardTags (
                CardId bigint NOT NULL,
                TagId bigint NOT NULL,
                CONSTRAINT PK_CardTags PRIMARY KEY (CardId, TagId),
                CONSTRAINT FK_CardTags_Cards FOREIGN KEY (CardId) REFERENCES Cards(Id) ON DELETE CASCADE,
                CONSTRAINT FK_CardTags_Tags FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
            )
            """);

        await ExecuteSchemaAsync("""
            IF OBJECT_ID(N'UploadFiles', N'U') IS NULL
            CREATE TABLE UploadFiles (
                Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                OriginalName nvarchar(255) NOT NULL,
                FileName nvarchar(255) NOT NULL,
                FileUrl nvarchar(500) NOT NULL,
                FileSize bigint NOT NULL,
                MimeType nvarchar(100) NOT NULL,
                UsageType nvarchar(50) NOT NULL,
                CreatedAt datetime2 NOT NULL CONSTRAINT DF_UploadFiles_CreatedAt DEFAULT SYSUTCDATETIME()
            )
            """);
    }

    private async Task SeedAsync()
    {
        if (Convert.ToInt32(await store.ScalarAsync("SELECT COUNT(*) FROM AdminUsers")) == 0)
        {
            var (hash, salt) = hasher.Create("Admin@123456");
            await store.ExecuteAsync("""
                INSERT INTO AdminUsers (Username, PasswordHash, PasswordSalt, Status, CreatedAt, UpdatedAt)
                VALUES (@Username, @Hash, @Salt, 1, SYSUTCDATETIME(), SYSUTCDATETIME())
                """, ("@Username", "admin"), ("@Hash", hash), ("@Salt", salt));
        }

        if (Convert.ToInt32(await store.ScalarAsync("SELECT COUNT(*) FROM SiteConfig")) == 0)
        {
            var (hash, salt) = hasher.Create("888888");
            await store.ExecuteAsync("""
                INSERT INTO SiteConfig (Id, SiteName, SiteSubtitle, SiteDescription, AccessPasswordHash, AccessPasswordSalt, AccessPasswordEnabled, UpdatedAt)
                VALUES (1, @SiteName, @Subtitle, @Description, @Hash, @Salt, 1, SYSUTCDATETIME())
                """,
                ("@SiteName", "梦梦杂货铺"),
                ("@Subtitle", "每日更新 稳定靠谱"),
                ("@Description", "认准梦梦不迷路"),
                ("@Hash", hash),
                ("@Salt", salt));
        }

        if (Convert.ToInt32(await store.ScalarAsync("SELECT COUNT(*) FROM Categories")) == 0)
        {
            var categories = new[]
            {
                new Category { Name = "水果", Slug = "fruit", SortOrder = 10, IsEnabled = true },
                new Category { Name = "零食", Slug = "snack", SortOrder = 20, IsEnabled = true },
                new Category { Name = "电器", Slug = "electronics", SortOrder = 30, IsEnabled = true },
                new Category { Name = "饮料", Slug = "drink", SortOrder = 40, IsEnabled = true },
                new Category { Name = "更多", Slug = "more", SortOrder = 50, IsEnabled = true }
            };

            foreach (var category in categories)
            {
                await store.SaveCategoryAsync(category);
            }
        }

        if (Convert.ToInt32(await store.ScalarAsync("SELECT COUNT(*) FROM Cards")) == 0)
        {
            var categories = await store.GetCategoriesAsync();
            long IdOf(string slug) => categories.First(c => c.Slug == slug).Id;

            var cards = new[]
            {
                new CardDetail
                {
                    CategoryId = IdOf("fruit"),
                    Title = "红富士苹果",
                    Summary = "新鲜脆甜",
                    CoverImageUrl = "/mobile/source/category-card-list-page/e05ac905d2780dae2654a7019cc89147.png",
                    Status = CardStatus.Published,
                    SortOrder = 10,
                    Tags = [new Tag { Name = "水果" }, new Tag { Name = "产地直供" }, new Tag { Name = "健康生活" }],
                    ContentHtml = """
                        <p>山东烟台红富士苹果以个大、色艳、味甜、汁多而闻名。这里光照充足、昼夜温差大，每一颗苹果都保留着清甜香气。</p>
                        <p>从采摘到包装，每一颗苹果都经过严格筛选，适合日常自用、家庭待客和节日礼盒搭配。</p>
                        <figure><img src="/mobile/source/article_detail/720x480.png" alt="烟台红富士苹果果园"><figcaption>烟台红富士苹果果园实景</figcaption></figure>
                        <p>果肉细腻多汁，甜中带微酸，入口清爽。每日一颗，健康生活从这里开始。</p>
                        <figure><img src="/mobile/source/article_detail/387cffcab176af62cd3eec220f8073a2.png" alt="优质苹果"><figcaption>刚采摘好的优质红富士苹果</figcaption></figure>
                        <p>认准当季好果，保留自然果香和脆甜口感。</p>
                        <figure><img src="/mobile/source/article_detail/4eb914729d91ede3e8cad25f35deb1cc.png" alt="苹果切面"><figcaption>果肉细腻多汁的红富士切面</figcaption></figure>
                        """
                },
                new CardDetail { CategoryId = IdOf("fruit"), Title = "赣南脐橙", Summary = "多汁香甜", CoverImageUrl = "/mobile/source/category-card-list-page/411fce1f241b1fd0f049586e1dacd7d5.png", Status = CardStatus.Published, SortOrder = 20, Tags = [new Tag { Name = "水果" }], ContentHtml = "<p>橙香浓郁，果肉饱满，适合鲜食和榨汁。</p>" },
                new CardDetail { CategoryId = IdOf("snack"), Title = "大辣片", Summary = "童年味道", CoverImageUrl = "/mobile/source/category-card-list-page/1aeb53d163450558e05b942c94770338.png", Status = CardStatus.Published, SortOrder = 30, Tags = [new Tag { Name = "零食" }], ContentHtml = "<p>经典辣味小零食，口感筋道，香辣过瘾。</p>" },
                new CardDetail { CategoryId = IdOf("snack"), Title = "薯片大礼包", Summary = "聚会必备", CoverImageUrl = "/mobile/source/category-card-list-page/3a77715a7f16d0f4266270a5ea063dbd.png", Status = CardStatus.Published, SortOrder = 40, Tags = [new Tag { Name = "零食" }, new Tag { Name = "聚会" }], ContentHtml = "<p>多口味组合装，适合朋友聚会、追剧和办公室分享。</p>" },
                new CardDetail { CategoryId = IdOf("electronics"), Title = "蓝牙耳机", Summary = "降噪长续航", CoverImageUrl = "/mobile/source/category-card-list-page/1b2542c1aa704f008d94266b4ccc8ad4.png", Status = CardStatus.Published, SortOrder = 50, Tags = [new Tag { Name = "电器" }], ContentHtml = "<p>佩戴轻巧，续航稳定，适合通勤和日常使用。</p>" },
                new CardDetail { CategoryId = IdOf("drink"), Title = "元气森林", Summary = "0糖气泡", CoverImageUrl = "/mobile/source/category-card-list-page/7f8e4c44efa2caf7140eeb5fe078c0b8.png", Status = CardStatus.Published, SortOrder = 60, Tags = [new Tag { Name = "饮料" }], ContentHtml = "<p>清爽气泡饮，轻负担口感，冰镇更佳。</p>" }
            };

            foreach (var card in cards)
            {
                await store.SaveCardAsync(card);
            }
        }
    }

    private async Task ExecuteSchemaAsync(string sql)
    {
        await using var connection = await store.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string value)
    {
        return "[" + value.Replace("]", "]]") + "]";
    }
}
