using GroceryStoreSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GroceryStoreSystem.Services;

public sealed class SqlDataStore(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is missing.");

    public string ConnectionString => _connectionString;

    public async Task<AdminUser?> GetAdminByUsernameAsync(string username)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 1 Id, Username, PasswordHash, PasswordSalt, Status, LastLoginAt
            FROM AdminUsers
            WHERE Username = @Username
            """;
        Add(command, "@Username", username);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AdminUser
        {
            Id = reader.GetInt64(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            PasswordSalt = reader.GetString(3),
            Status = reader.GetInt32(4),
            LastLoginAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
        };
    }

    public async Task TouchAdminLoginAsync(long id)
    {
        await ExecuteAsync("UPDATE AdminUsers SET LastLoginAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            ("@Id", id));
    }

    public async Task ChangeAdminPasswordAsync(long id, string hash, string salt)
    {
        await ExecuteAsync("""
            UPDATE AdminUsers
            SET PasswordHash = @Hash, PasswordSalt = @Salt, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """, ("@Id", id), ("@Hash", hash), ("@Salt", salt));
    }

    public async Task<SiteConfig> GetSiteConfigAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP 1 Id, SiteName, SiteSubtitle, SiteDescription,
                   AccessPasswordHash, AccessPasswordSalt, AccessPasswordEnabled, UpdatedAt
            FROM SiteConfig
            WHERE Id = 1
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new SiteConfig();
        }

        return new SiteConfig
        {
            Id = reader.GetInt64(0),
            SiteName = reader.GetString(1),
            SiteSubtitle = reader.GetString(2),
            SiteDescription = reader.GetString(3),
            AccessPasswordHash = reader.GetString(4),
            AccessPasswordSalt = reader.GetString(5),
            AccessPasswordEnabled = reader.GetBoolean(6),
            UpdatedAt = reader.GetDateTime(7)
        };
    }

    public async Task SaveSiteConfigAsync(SiteConfig config, string? newAccessPassword, PasswordHasher hasher)
    {
        var hash = config.AccessPasswordHash;
        var salt = config.AccessPasswordSalt;

        if (!string.IsNullOrWhiteSpace(newAccessPassword))
        {
            (hash, salt) = hasher.Create(newAccessPassword);
        }

        await ExecuteAsync("""
            UPDATE SiteConfig
            SET SiteName = @SiteName,
                SiteSubtitle = @SiteSubtitle,
                SiteDescription = @SiteDescription,
                AccessPasswordHash = @Hash,
                AccessPasswordSalt = @Salt,
                AccessPasswordEnabled = @Enabled,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = 1
            """,
            ("@SiteName", config.SiteName),
            ("@SiteSubtitle", config.SiteSubtitle),
            ("@SiteDescription", config.SiteDescription),
            ("@Hash", hash),
            ("@Salt", salt),
            ("@Enabled", config.AccessPasswordEnabled));
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(bool enabledOnly = false)
    {
        var items = new List<Category>();
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Id, Name, Slug, Icon, SortOrder, IsEnabled, CreatedAt, UpdatedAt
            FROM Categories
            {(enabledOnly ? "WHERE IsEnabled = 1" : "")}
            ORDER BY SortOrder, Id
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Category
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Slug = reader.GetString(2),
                Icon = reader.IsDBNull(3) ? null : reader.GetString(3),
                SortOrder = reader.GetInt32(4),
                IsEnabled = reader.GetBoolean(5),
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.GetDateTime(7)
            });
        }

        return items;
    }

    public async Task<Category?> GetCategoryAsync(long id)
    {
        var items = await GetCategoriesAsync();
        return items.FirstOrDefault(item => item.Id == id);
    }

    public async Task SaveCategoryAsync(Category category)
    {
        if (category.Id == 0)
        {
            await ExecuteAsync("""
                INSERT INTO Categories (Name, Slug, Icon, SortOrder, IsEnabled, CreatedAt, UpdatedAt)
                VALUES (@Name, @Slug, @Icon, @SortOrder, @IsEnabled, SYSUTCDATETIME(), SYSUTCDATETIME())
                """,
                ("@Name", category.Name),
                ("@Slug", category.Slug),
                ("@Icon", category.Icon),
                ("@SortOrder", category.SortOrder),
                ("@IsEnabled", category.IsEnabled));
            return;
        }

        await ExecuteAsync("""
            UPDATE Categories
            SET Name = @Name,
                Slug = @Slug,
                Icon = @Icon,
                SortOrder = @SortOrder,
                IsEnabled = @IsEnabled,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            ("@Id", category.Id),
            ("@Name", category.Name),
            ("@Slug", category.Slug),
            ("@Icon", category.Icon),
            ("@SortOrder", category.SortOrder),
            ("@IsEnabled", category.IsEnabled));
    }

    public async Task DeleteCategoryAsync(long id)
    {
        await ExecuteAsync("DELETE FROM Categories WHERE Id = @Id", ("@Id", id));
    }

    public async Task<IReadOnlyList<Tag>> GetTagsAsync()
    {
        var items = new List<Tag>();
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Color, CreatedAt FROM Tags ORDER BY Name";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Tag
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Color = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return items;
    }

    public async Task SaveTagAsync(Tag tag)
    {
        if (tag.Id == 0)
        {
            await ExecuteAsync("""
                IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = @Name)
                INSERT INTO Tags (Name, Color, CreatedAt) VALUES (@Name, @Color, SYSUTCDATETIME())
                """, ("@Name", tag.Name), ("@Color", tag.Color));
            return;
        }

        await ExecuteAsync("UPDATE Tags SET Name = @Name, Color = @Color WHERE Id = @Id",
            ("@Id", tag.Id), ("@Name", tag.Name), ("@Color", tag.Color));
    }

    public async Task DeleteTagAsync(long id)
    {
        await ExecuteAsync("DELETE FROM CardTags WHERE TagId = @Id; DELETE FROM Tags WHERE Id = @Id", ("@Id", id));
    }

    public async Task<IReadOnlyList<CardListItem>> GetCardsAsync(long? categoryId = null, int? status = null, string? keyword = null, bool mobileOnly = false)
    {
        var items = new List<CardListItem>();
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.Id, c.CategoryId, cat.Name AS CategoryName, c.Title, c.Summary, c.CoverImageUrl,
                   c.Status, c.SortOrder,
                   COALESCE(STRING_AGG(t.Name, N','), N'') AS Tags,
                   c.PublishedAt, c.UpdatedAt
            FROM Cards c
            INNER JOIN Categories cat ON cat.Id = c.CategoryId
            LEFT JOIN CardTags ct ON ct.CardId = c.Id
            LEFT JOIN Tags t ON t.Id = ct.TagId
            WHERE (@CategoryId IS NULL OR c.CategoryId = @CategoryId)
              AND (@Status IS NULL OR c.Status = @Status)
              AND (@Keyword IS NULL OR c.Title LIKE @Keyword OR c.Summary LIKE @Keyword)
              AND (@MobileOnly = 0 OR (c.Status = 1 AND cat.IsEnabled = 1))
            GROUP BY c.Id, c.CategoryId, cat.Name, c.Title, c.Summary, c.CoverImageUrl,
                     c.Status, c.SortOrder, c.PublishedAt, c.UpdatedAt
            ORDER BY c.SortOrder, c.Id DESC
            """;
        Add(command, "@CategoryId", categoryId);
        Add(command, "@Status", status);
        Add(command, "@Keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");
        Add(command, "@MobileOnly", mobileOnly);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new CardListItem
            {
                Id = reader.GetInt64(0),
                CategoryId = reader.GetInt64(1),
                CategoryName = reader.GetString(2),
                Title = reader.GetString(3),
                Summary = reader.GetString(4),
                CoverImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetInt32(6),
                SortOrder = reader.GetInt32(7),
                Tags = reader.GetString(8),
                PublishedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                UpdatedAt = reader.GetDateTime(10)
            });
        }

        return items;
    }

    public async Task<CardDetail?> GetCardAsync(long id, bool mobileOnly = false)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.Id, c.CategoryId, cat.Name AS CategoryName, c.Title, c.Summary, c.CoverImageUrl,
                   c.ContentHtml, c.Status, c.SortOrder, c.PublishedAt, c.UpdatedAt
            FROM Cards c
            INNER JOIN Categories cat ON cat.Id = c.CategoryId
            WHERE c.Id = @Id
              AND (@MobileOnly = 0 OR (c.Status = 1 AND cat.IsEnabled = 1))
            """;
        Add(command, "@Id", id);
        Add(command, "@MobileOnly", mobileOnly);

        CardDetail? card = null;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                card = new CardDetail
                {
                    Id = reader.GetInt64(0),
                    CategoryId = reader.GetInt64(1),
                    CategoryName = reader.GetString(2),
                    Title = reader.GetString(3),
                    Summary = reader.GetString(4),
                    CoverImageUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ContentHtml = reader.GetString(6),
                    Status = reader.GetInt32(7),
                    SortOrder = reader.GetInt32(8),
                    PublishedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    UpdatedAt = reader.GetDateTime(10)
                };
            }
        }

        if (card is null)
        {
            return null;
        }

        card.Tags = await GetCardTagsAsync(connection, id);
        return card;
    }

    public async Task<long> SaveCardAsync(CardDetail card)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            if (card.Id == 0)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = (SqlTransaction)transaction;
                insert.CommandText = """
                    INSERT INTO Cards (CategoryId, Title, Summary, CoverImageUrl, ContentHtml, Status, SortOrder, PublishedAt, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@CategoryId, @Title, @Summary, @CoverImageUrl, @ContentHtml, @Status, @SortOrder,
                            CASE WHEN @Status = 1 THEN SYSUTCDATETIME() ELSE NULL END, SYSUTCDATETIME(), SYSUTCDATETIME())
                    """;
                Add(insert, "@CategoryId", card.CategoryId);
                Add(insert, "@Title", card.Title);
                Add(insert, "@Summary", card.Summary);
                Add(insert, "@CoverImageUrl", card.CoverImageUrl);
                Add(insert, "@ContentHtml", card.ContentHtml);
                Add(insert, "@Status", card.Status);
                Add(insert, "@SortOrder", card.SortOrder);
                card.Id = Convert.ToInt64(await insert.ExecuteScalarAsync());
            }
            else
            {
                await using var update = connection.CreateCommand();
                update.Transaction = (SqlTransaction)transaction;
                update.CommandText = """
                    UPDATE Cards
                    SET CategoryId = @CategoryId,
                        Title = @Title,
                        Summary = @Summary,
                        CoverImageUrl = @CoverImageUrl,
                        ContentHtml = @ContentHtml,
                        Status = @Status,
                        SortOrder = @SortOrder,
                        PublishedAt = CASE WHEN @Status = 1 AND PublishedAt IS NULL THEN SYSUTCDATETIME() ELSE PublishedAt END,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id
                    """;
                Add(update, "@Id", card.Id);
                Add(update, "@CategoryId", card.CategoryId);
                Add(update, "@Title", card.Title);
                Add(update, "@Summary", card.Summary);
                Add(update, "@CoverImageUrl", card.CoverImageUrl);
                Add(update, "@ContentHtml", card.ContentHtml);
                Add(update, "@Status", card.Status);
                Add(update, "@SortOrder", card.SortOrder);
                await update.ExecuteNonQueryAsync();
            }

            await SaveCardTagsAsync(connection, (SqlTransaction)transaction, card.Id, card.Tags.Select(t => t.Name));
            await transaction.CommitAsync();
            return card.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteCardAsync(long id)
    {
        await ExecuteAsync("DELETE FROM CardTags WHERE CardId = @Id; DELETE FROM Cards WHERE Id = @Id", ("@Id", id));
    }

    public async Task SetCardStatusAsync(long id, int status)
    {
        await ExecuteAsync("""
            UPDATE Cards
            SET Status = @Status,
                PublishedAt = CASE WHEN @Status = 1 AND PublishedAt IS NULL THEN SYSUTCDATETIME() ELSE PublishedAt END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """, ("@Id", id), ("@Status", status));
    }

    public async Task SaveUploadAsync(UploadFileRecord file)
    {
        await ExecuteAsync("""
            INSERT INTO UploadFiles (OriginalName, FileName, FileUrl, FileSize, MimeType, UsageType, CreatedAt)
            VALUES (@OriginalName, @FileName, @FileUrl, @FileSize, @MimeType, @UsageType, SYSUTCDATETIME())
            """,
            ("@OriginalName", file.OriginalName),
            ("@FileName", file.FileName),
            ("@FileUrl", file.FileUrl),
            ("@FileSize", file.FileSize),
            ("@MimeType", file.MimeType),
            ("@UsageType", file.UsageType));
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM Categories),
                (SELECT COUNT(*) FROM Cards),
                (SELECT COUNT(*) FROM Cards WHERE Status = 1),
                (SELECT MAX(UpdatedAt) FROM Cards)
            """;

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new DashboardStats
        {
            CategoryCount = reader.GetInt32(0),
            CardCount = reader.GetInt32(1),
            PublishedCount = reader.GetInt32(2),
            LastUpdatedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
        };
    }

    public async Task<int> CountCardsByCategoryAsync(long categoryId)
    {
        var value = await ScalarAsync("SELECT COUNT(*) FROM Cards WHERE CategoryId = @CategoryId", ("@CategoryId", categoryId));
        return Convert.ToInt32(value);
    }

    public async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Value);
        }

        await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ScalarAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Value);
        }

        return await command.ExecuteScalarAsync();
    }

    private static async Task<List<Tag>> GetCardTagsAsync(SqlConnection connection, long cardId)
    {
        var items = new List<Tag>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.Id, t.Name, t.Color, t.CreatedAt
            FROM Tags t
            INNER JOIN CardTags ct ON ct.TagId = t.Id
            WHERE ct.CardId = @CardId
            ORDER BY t.Name
            """;
        Add(command, "@CardId", cardId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Tag
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Color = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return items;
    }

    private static async Task SaveCardTagsAsync(SqlConnection connection, SqlTransaction transaction, long cardId, IEnumerable<string> tagNames)
    {
        await using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM CardTags WHERE CardId = @CardId";
        Add(delete, "@CardId", cardId);
        await delete.ExecuteNonQueryAsync();

        foreach (var rawName in tagNames.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await using var ensure = connection.CreateCommand();
            ensure.Transaction = transaction;
            ensure.CommandText = """
                IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = @Name)
                    INSERT INTO Tags (Name, Color, CreatedAt) VALUES (@Name, NULL, SYSUTCDATETIME());
                SELECT Id FROM Tags WHERE Name = @Name;
                """;
            Add(ensure, "@Name", rawName);
            var tagId = Convert.ToInt64(await ensure.ExecuteScalarAsync());

            await using var link = connection.CreateCommand();
            link.Transaction = transaction;
            link.CommandText = "INSERT INTO CardTags (CardId, TagId) VALUES (@CardId, @TagId)";
            Add(link, "@CardId", cardId);
            Add(link, "@TagId", tagId);
            await link.ExecuteNonQueryAsync();
        }
    }

    private static void Add(SqlCommand command, string name, object? value)
    {
        command.Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));
    }
}
